using System.Collections.Generic;
using EFT;
using Newtonsoft.Json;

namespace BatteriesNotIncluded.Models;

public record ModConfig
{
    [JsonProperty("deviceBatteryData")]
    public Dictionary<string, DeviceData> DeviceBatteryData { get; set; } = [];

    [JsonProperty("botBatteries")]
    public Dictionary<WildSpawnType, RangedInt> BotBatteries { get; set; } = [];

    [JsonProperty("tacticalDevicesDrain")]
    public Dictionary<DeviceMode, float> TacticalDevicesDrain { get; set; } = [];
}
