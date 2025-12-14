using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace BatteriesNotIncluded.Models;

public record DeviceData
{
    [JsonPropertyName("battery")]
    public MongoId? Battery { get; set; } = null;
    
    [JsonPropertyName("slots")]
    public int Slots { get; set; } = 1;
    
    [JsonPropertyName("drainRate")]
    public float DrainRate { get; set; } = 1f;

    public static DeviceData Empty => new();
}
