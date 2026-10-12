using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

/// <summary>One window of satellite numbers over a block.</summary>
public record SatelliteObservation(DateTime From, DateTime To, double Ndvi, double Ndmi, double ValidShare);

public enum SatelliteLayer { TrueColour, Ndvi, Ndmi }

public interface ISatelliteService
{
    /// <summary>False until the Copernicus client id and secret are in the settings; nothing is fetched then.</summary>
    bool IsConfigured { get; }

    /// <summary>NDVI and NDMI over the block's outline, one row per 5-day window between the two dates; windows with no clear view are left out.</summary>
    Task<IReadOnlyList<SatelliteObservation>> GetBlockStatsAsync(string boundaryGeoJson, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>A PNG of the block (transparent outside its outline) for one layer, using the least cloudy view between the two dates.</summary>
    Task<byte[]> GetImageAsync(string boundaryGeoJson, SatelliteLayer layer, DateTime from, DateTime to, int width, int height, CancellationToken ct = default);
}

public interface IBlockSatelliteRepository
{
    Task<List<BlockSatelliteReading>> GetByFarmAsync(string farmId, DateTime since, CancellationToken ct = default);
    Task<List<BlockSatelliteReading>> GetByBlockAsync(string blockId, DateTime since, CancellationToken ct = default);
    Task UpsertAsync(string farmId, string blockId, IEnumerable<SatelliteObservation> observations, CancellationToken ct = default);
}
