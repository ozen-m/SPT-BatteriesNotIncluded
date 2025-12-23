using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace BatteriesNotIncluded.Models;

public record DeviceData
{
    [JsonPropertyName("batteryId")]
    public MongoId Battery { get; set; } = new("5672cb304bdc2dc2088b456a");
    
    [JsonPropertyName("slots")]
    public int SlotCount { get; set; } = 1;
    
    [JsonPropertyName("runtime")]
    public double RealRuntimeHr { get; set; } = 100_000d;
    
    [JsonPropertyName("gameRuntime")]
    public double GameRuntimeSecs { get; set; } = 9000d;
    
    [JsonPropertyName("drainPerSecond")]
    public double DrainPerSecond { get; set; } = 0.01d;

    public static DeviceData Default => new();
}
