using Newtonsoft.Json;

namespace BatteriesNotIncluded.Models;

public struct DeviceData
{
    [JsonProperty("batteryId")]
    public string Battery { get; set; }

    [JsonProperty("slots")]
    public int SlotCount { get; set; }

    [JsonProperty("drainPerSecond")]
    public float DrainPerSecond { get; set; }
}
