using System.Collections.Generic;
using EFT;
using Newtonsoft.Json;

namespace BatteriesNotIncluded.Models;

public record ModConfig
{
    [JsonProperty("deviceBatteryData")]
    public Dictionary<MongoID, DeviceData> DeviceBatteryData { get; set; } = [];

    [JsonProperty("botBatteries")]
    public Dictionary<WildSpawnType, RangedInt> BotBatteries { get; set; } = [];

    [JsonProperty("tacticalDevicesDrain")]
    public Dictionary<DeviceMode, float> TacticalDevicesDrain { get; set; } = [];
    
    [JsonProperty("tacticalDevicesModeOverride")]
    public Dictionary<MongoID, Dictionary<string, DeviceMode>> TacticalDevicesModeOverride { get; set; } = [];
}
