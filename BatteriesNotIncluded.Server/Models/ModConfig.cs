using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;

// ReSharper disable CollectionNeverUpdated.Global

namespace BatteriesNotIncluded.Models;

public record ModConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("globalDrainMultiplier")]
    public double GlobalDrainMult { get; set; } = 1d;

    [JsonPropertyName("minGameRuntime")]
    public double MinGameRuntime { get; set; } = 900d;

    [JsonPropertyName("maxGameRuntime")]
    public double MaxGameRuntime { get; set; } = 9_000d;

    [JsonPropertyName("botBatteries")]
    public Dictionary<WildSpawnType, MinMax<int>> BotBatteries { get; set; } = [];

    [JsonPropertyName("siccContainerBatteries")]
    public bool SiccContainerBatteries { get; set; } = true;

    [JsonPropertyName("debugLogs")]
    public bool DebugLogs { get; set; } = false;

    [JsonPropertyName("tacticalDevicesRuntime")]
    public Dictionary<DeviceMode, double> TacticalDevicesDrain { get; set; } = [];

    [JsonPropertyName("deviceBatteryData")]
    public Dictionary<MongoId, Dictionary<MongoId, DeviceData>> DeviceBatteryData { get; set; } = [];
}
