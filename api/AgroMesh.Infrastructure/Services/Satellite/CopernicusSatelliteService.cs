using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using Microsoft.Extensions.Configuration;

namespace AgroMesh.Infrastructure.Services.Satellite;

/// <summary>
/// NDVI and NDMI for a block from Sentinel-2, through the free Sentinel Hub service of the Copernicus Data Space Ecosystem
/// (Statistical API): the block's outline goes in, one average per 5-day window comes back, so no imagery is downloaded.
/// Clouds, cloud shadow, snow and water are masked out using the scene classification layer.
/// </summary>
public class CopernicusSatelliteService : ISatelliteService
{
    private const string TokenUrl = "https://identity.dataspace.copernicus.eu/auth/realms/CDSE/protocol/openid-connect/token";
    private const string StatisticsUrl = "https://sh.dataspace.copernicus.eu/statistics/v1";

    // Pixel size in degrees (EPSG:4326): about 11 m near the equator, matching Sentinel-2's 10 m bands.
    private const double PixelDegrees = 0.0001;

    // SCL classes kept: 4 vegetation, 5 bare soil, 7 unclassified. Everything else (no data, saturated, shadow, water, cloud, cirrus, snow) is dropped.
    private const string EvalScript = """
        //VERSION=3
        function setup() {
          return {
            input: [{ bands: ["B04", "B08", "B11", "SCL", "dataMask"] }],
            output: [
              { id: "ndvi", bands: 1 },
              { id: "ndmi", bands: 1 },
              { id: "dataMask", bands: 1 }
            ]
          };
        }
        function evaluatePixel(s) {
          var clear = (s.SCL == 4 || s.SCL == 5 || s.SCL == 7) ? 1 : 0;
          var okNdvi = (s.B08 + s.B04) == 0 ? 0 : 1;
          var okNdmi = (s.B08 + s.B11) == 0 ? 0 : 1;
          return {
            ndvi: [(s.B08 - s.B04) / (s.B08 + s.B04)],
            ndmi: [(s.B08 - s.B11) / (s.B08 + s.B11)],
            dataMask: [s.dataMask * clear * okNdvi * okNdmi]
          };
        }
        """;

    private const string ProcessUrl = "https://sh.dataspace.copernicus.eu/api/v1/process";

    // Pictures are RGBA: the last band is transparency, 0 outside the block's outline (and over cloud for the index layers).
    private const string PictureHead = """
        //VERSION=3
        function setup() {
          return { input: [{ bands: ["B02", "B03", "B04", "B08", "B11", "SCL", "dataMask"] }], output: { bands: 4 } };
        }
        function ramp(v, stops) { for (var i = stops.length - 1; i >= 0; i--) { if (v >= stops[i][0]) return stops[i][1]; } return stops[0][1]; }
        function clear(s) { return (s.SCL == 4 || s.SCL == 5 || s.SCL == 7) ? 1 : 0; }
        """;

    private static string PictureScript(SatelliteLayer layer) => PictureHead + layer switch
    {
        // Honest colour picture: clouds show up as clouds.
        SatelliteLayer.TrueColour => """
            function evaluatePixel(s) { return [2.5 * s.B04, 2.5 * s.B03, 2.5 * s.B02, s.dataMask]; }
            """,
        // Greenness: brown (bare) through tan and light green to dark green (dense crop).
        SatelliteLayer.Ndvi => """
            var C = [[-1,[0.55,0.45,0.35]],[0.1,[0.85,0.78,0.55]],[0.25,[0.75,0.85,0.45]],[0.4,[0.40,0.72,0.30]],[0.55,[0.15,0.55,0.20]],[0.7,[0.02,0.35,0.12]]];
            function evaluatePixel(s) { var c = ramp((s.B08 - s.B04) / (s.B08 + s.B04), C); return [c[0], c[1], c[2], s.dataMask * clear(s)]; }
            """,
        // Leaf water: red-brown (dry) through yellow to deep blue (wet).
        _ => """
            var C = [[-1,[0.65,0.30,0.20]],[0.0,[0.90,0.65,0.25]],[0.1,[0.95,0.90,0.45]],[0.2,[0.55,0.82,0.85]],[0.3,[0.25,0.60,0.85]],[0.45,[0.05,0.25,0.65]]];
            function evaluatePixel(s) { var c = ramp((s.B08 - s.B11) / (s.B08 + s.B11), C); return [c[0], c[1], c[2], s.dataMask * clear(s)]; }
            """,
    };

    // One token serves every request until shortly before it expires.
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static string? _token;
    private static DateTime _tokenExpires = DateTime.MinValue;

    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public CopernicusSatelliteService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _http = httpFactory.CreateClient("copernicus-client");
        _clientId = config["Copernicus:ClientId"] ?? "";
        _clientSecret = config["Copernicus:ClientSecret"] ?? "";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret);

    public async Task<IReadOnlyList<SatelliteObservation>> GetBlockStatsAsync(string boundaryGeoJson, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (!IsConfigured) return [];

        using var boundary = JsonDocument.Parse(boundaryGeoJson);
        var body = new
        {
            input = new
            {
                bounds = new
                {
                    geometry = boundary.RootElement.Clone(),
                    properties = new { crs = "http://www.opengis.net/def/crs/EPSG/0/4326" },
                },
                data = new[] { new { type = "sentinel-2-l2a", dataFilter = new { mosaickingOrder = "leastCC" } } },
            },
            aggregation = new
            {
                timeRange = new { from = from.ToString("yyyy-MM-dd'T'00:00:00'Z'"), to = to.ToString("yyyy-MM-dd'T'00:00:00'Z'") },
                aggregationInterval = new { of = "P5D" },
                lastIntervalBehavior = "SHORTEN",
                evalscript = EvalScript,
                resx = PixelDegrees,
                resy = PixelDegrees,
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, StatisticsUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Copernicus returned {(int)response.StatusCode}: {(text.Length > 300 ? text[..300] : text)}");
        return SatelliteStatistics.Parse(text);
    }

    public async Task<byte[]> GetImageAsync(string boundaryGeoJson, SatelliteLayer layer, DateTime from, DateTime to, int width, int height, CancellationToken ct = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("Satellite monitoring is not configured.");

        using var boundary = JsonDocument.Parse(boundaryGeoJson);
        var body = new
        {
            input = new
            {
                bounds = new
                {
                    geometry = boundary.RootElement.Clone(),
                    properties = new { crs = "http://www.opengis.net/def/crs/EPSG/0/4326" },
                },
                data = new[]
                {
                    new
                    {
                        type = "sentinel-2-l2a",
                        dataFilter = new
                        {
                            timeRange = new { from = from.ToString("yyyy-MM-dd'T'00:00:00'Z'"), to = to.ToString("yyyy-MM-dd'T'00:00:00'Z'") },
                            mosaickingOrder = "leastCC",
                        },
                    },
                },
            },
            output = new { width, height, responses = new[] { new { identifier = "default", format = new { type = "image/png" } } } },
            evalscript = PictureScript(layer),
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ProcessUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Copernicus returned {(int)response.StatusCode}: {(text.Length > 300 ? text[..300] : text)}");
        }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token is not null && DateTime.UtcNow < _tokenExpires) return _token;

        await TokenLock.WaitAsync(ct);
        try
        {
            if (_token is not null && DateTime.UtcNow < _tokenExpires) return _token;

            using var response = await _http.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
            }), ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            // Never include the reply body for a rejected login: keep the message to the status only.
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Copernicus sign-in failed ({(int)response.StatusCode}). Check the client id and secret.");

            using var doc = JsonDocument.Parse(text);
            _token = doc.RootElement.GetProperty("access_token").GetString()!;
            var seconds = doc.RootElement.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 600;
            _tokenExpires = DateTime.UtcNow.AddSeconds(Math.Max(30, seconds - 60));
            return _token;
        }
        finally
        {
            TokenLock.Release();
        }
    }
}
