using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;

#pragma warning disable CA2211

namespace BatteriesNotIncluded;

[BepInPlugin("com.ozen.batteriesnotincluded", "Batteries Not Included", "1.0.2")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
public class BatteriesNotIncluded : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static ConfigEntry<bool> ShowRemainingBattery;
    public static ConfigEntry<bool> DebugLogs;

    private static Dictionary<MongoID, DeviceData> _deviceBatteryData = [];
    private static Dictionary<WildSpawnType, RangedInt> _botBatteries = [];
    private static Dictionary<DeviceMode, float> _tacticalDevicesDrain = [];
    private static Dictionary<MongoID, Dictionary<string, DeviceMode>> _tacticalDevicesOverride = [];

    private static PatchManager _patchManager;

    protected void Awake()
    {
        LogSource = Logger;

        CheckForPrepatch();

        ShowRemainingBattery = Config.Bind("General", "Remaining Battery Tooltip", true, new ConfigDescription("Show remaining runtime when hovering over a device", null, new ConfigurationManagerAttributes() { Order = 1 }));
        DebugLogs = Config.Bind("Debug", "Logging", false, new ConfigDescription("Show debug logs", null, new ConfigurationManagerAttributes() { IsAdvanced = true, Order = 0 }));

        Fika.IsFikaPresent = Chainloader.PluginInfos.ContainsKey("com.fika.core");

        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();

        _ = Task.Run(() => _ = GetConfigFromServerAsync());
    }

    public static bool GetDeviceData(MongoID templateId, out DeviceData deviceData) =>
        _deviceBatteryData.TryGetValue(templateId, out deviceData);

    public static RangedInt GetBotRange(WildSpawnType wildSpawnType) =>
        _botBatteries.GetValueOrDefault(wildSpawnType, _defaultRange);

    public static float GetTacticalDrain(DeviceMode mode)
    {
        if (_tacticalDevicesDrain.TryGetValue(mode, out var drain))
        {
            return drain;
        }

        LoggerUtil.Warning($"Missing DrainPerSecond for mode: {mode.ToString()}");
        return 0.011111f;
    }

    public static DeviceMode GetDeviceModeOverride(MongoID templateId, string mode)
    {
        return _tacticalDevicesOverride.TryGetValue(templateId, out var deviceModeOverrides) &&
               deviceModeOverrides.TryGetValue(mode, out var deviceMode)
            ? deviceMode
            : DeviceMode.None;
    }

    public static void DisablePatches() => _patchManager.DisablePatches();

    private static async Task GetConfigFromServerAsync()
    {
        bool error = false;
        try
        {
            string json = await RequestHandler.GetJsonAsync("/BatteriesNotIncluded/GetConfig");
            if (string.IsNullOrWhiteSpace(json))
            {
                error = true;
            }

            var modConfig = JsonConvert.DeserializeObject<ModConfig>(json!);
            _deviceBatteryData = modConfig.DeviceBatteryData;
            _botBatteries = modConfig.BotBatteries;
            _tacticalDevicesDrain = modConfig.TacticalDevicesDrain;
            _tacticalDevicesOverride = modConfig.TacticalDevicesModeOverride;

            if (_deviceBatteryData.IsNullOrEmpty() ||
                _botBatteries.IsNullOrEmpty() ||
                _tacticalDevicesDrain.IsNullOrEmpty() ||
                _tacticalDevicesOverride.IsNullOrEmpty())
            {
                error = true;
            }
        }
        catch (Exception)
        {
            error = true;
        }

        if (error)
        {
            DisablePatches();
            LoggerUtil.Error("Could not get configuration files from the server. Disabled mod Batteries Not Included.");
            return;
        }

        LoggerUtil.Info($"Successfully fetched {_deviceBatteryData.Count} battery operated devices!");
    }

    private static void CheckForPrepatch()
    {
        try
        {
            HeadphonesTogglableField = AccessTools.FieldRefAccess<HeadphonesItemClass, TogglableComponent>("Togglable");
        }
        catch
        {
            // Ignored
        }
    }

    public static AccessTools.FieldRef<HeadphonesItemClass, TogglableComponent> HeadphonesTogglableField;
    private static readonly RangedInt _defaultRange = new(40, 60);
}
