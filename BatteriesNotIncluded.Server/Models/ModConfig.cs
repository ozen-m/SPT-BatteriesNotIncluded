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

    [JsonPropertyName("batteries")]
    public Dictionary<MongoId, Dictionary<MongoId, DeviceData>> Batteries { get; set; } = [];
}
