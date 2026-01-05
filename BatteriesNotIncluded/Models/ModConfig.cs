using System.Collections.Generic;
using EFT;
using Newtonsoft.Json;

namespace BatteriesNotIncluded.Models;

public record ModConfig
{
    [JsonProperty("deviceBatteryDefinitions")]
    public Dictionary<MongoID, DeviceData> DeviceBatteryDefinitions { get; set; } = [];

    [JsonProperty("botBatteries")]
    public Dictionary<WildSpawnType, RangedInt> BotBatteries { get; set; } = [];

    [JsonProperty("tacticalDevicesDrain")]
    public Dictionary<DeviceMode, float> TacticalDevicesDrain { get; set; } = [];
    
    [JsonProperty("tacticalDevicesModeOverride")]
    public Dictionary<MongoID, Dictionary<string, DeviceMode>> TacticalDevicesModeOverride { get; set; } = [];
}
