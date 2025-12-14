using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

// ReSharper disable CollectionNeverUpdated.Global

namespace BatteriesNotIncluded.Models;

public record ModConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("debugLogs")]
    public bool DebugLogs { get; set; } = false;

    // [JsonPropertyName("batteries")]
    // public Dictionary<MongoId, HashSet<MongoId>> Batteries { get; set; } = [];

    [JsonPropertyName("cr123a")]
    public Dictionary<MongoId, DeviceData> CR123A { get; set; } = [];

    [JsonPropertyName("cr2032")]
    public Dictionary<MongoId, DeviceData> CR2032 { get; set; } = [];

    [JsonPropertyName("aa")]
    public Dictionary<MongoId, DeviceData> AA { get; set; } = [];

    [JsonPropertyName("noBattery")]
    public Dictionary<MongoId, DeviceData> NoBattery { get; set; } = [];
}
