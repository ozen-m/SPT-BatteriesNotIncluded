using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace BatteriesNotIncluded.Models;

public record DeviceData
{
    [JsonPropertyName("batteryId")]
    public MongoId Battery { get; set; } = new("5672cb304bdc2dc2088b456a");
    
    [JsonPropertyName("slotCount")]
    public int SlotCount { get; set; } = 1;
    
    [JsonPropertyName("drainRate")]
    public float DrainRate { get; set; } = 1f;

    public static DeviceData Default => new();
}
