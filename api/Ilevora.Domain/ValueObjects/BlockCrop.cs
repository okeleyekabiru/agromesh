using System.Text.Json;
using System.Text.Json.Serialization;
using Ilevora.Domain.Crops;

namespace Ilevora.Domain.ValueObjects;

/// <summary>
/// One crop on a block. CropId is a built-in code (OilPalm, Maize...) or the id of a farm's custom crop;
/// CropName is its display name, kept here so reports need no lookup. Stored as JSON on the block; rows
/// written before the Crops table ({"CropType":0,...}) read back losslessly through <see cref="BlockCropJsonConverter"/>.
/// </summary>
[JsonConverter(typeof(BlockCropJsonConverter))]
public record BlockCrop(string CropId, string CropName, double Hectares, int Percentage)
{
    public BlockCrop(BuiltInCrop crop, double hectares, int percentage) : this(crop.Id, crop.Name, hectares, percentage) { }

    [JsonIgnore]
    public BuiltInCrop? BuiltIn => BuiltInCrop.FromId(CropId);

    /// <summary>The name crop figures are grouped under: the built-in code (as before) or the custom crop name.</summary>
    [JsonIgnore]
    public string Key => BuiltIn?.Id ?? CropName;
}

public class BlockCropJsonConverter : JsonConverter<BlockCrop>
{
    public override BlockCrop Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        string? id = null, name = null;
        double hectares = 0; int pct = 0;
        foreach (var p in doc.RootElement.EnumerateObject())
        {
            switch (p.Name.ToLowerInvariant())
            {
                case "cropid": id = p.Value.GetString(); break;
                case "cropname": name = p.Value.GetString(); break;
                case "hectares": hectares = p.Value.GetDouble(); break;
                case "percentage": pct = p.Value.GetInt32(); break;
                case "croptype": // legacy enum: ordinal number or member name
                    if (id is null)
                    {
                        var legacy = p.Value.ValueKind == JsonValueKind.Number
                            ? BuiltInCrop.FromLegacyOrdinal(p.Value.GetInt32())
                            : BuiltInCrop.FromId(p.Value.GetString());
                        id = legacy?.Id ?? p.Value.ToString();
                    }
                    break;
            }
        }
        id ??= "";
        name ??= BuiltInCrop.FromId(id)?.Name ?? id;
        return new BlockCrop(id, name, hectares, pct);
    }

    public override void Write(Utf8JsonWriter writer, BlockCrop value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("CropId", value.CropId);
        writer.WriteString("CropName", value.CropName);
        writer.WriteNumber("Hectares", value.Hectares);
        writer.WriteNumber("Percentage", value.Percentage);
        writer.WriteEndObject();
    }
}
