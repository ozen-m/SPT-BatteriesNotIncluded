using Newtonsoft.Json;

namespace BatteriesNotIncluded.Models;

public struct DeviceData
{
    [JsonProperty("batteryId")]
    public string Battery { get; set; }

    [JsonProperty("slotCount")]
    public int SlotCount { get; set; }

    [JsonProperty("drainRate")]
    public float DrainRate { get; set; }
}
