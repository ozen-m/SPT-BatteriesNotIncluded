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

[BepInPlugin("com.ozen.batteriesnotincluded", "Batteries Not Included", "1.0.1")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
public class BatteriesNotIncluded : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static ConfigEntry<bool> ShowRemainingBattery;
    public static ConfigEntry<bool> DebugLogs;

    private static Dictionary<string, DeviceData> _deviceData = [];
    private static Dictionary<WildSpawnType, RangedInt> _botBatteries = [];

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

    public static bool GetDeviceData(string deviceId, out DeviceData deviceData) =>
        _deviceData.TryGetValue(deviceId, out deviceData);

    public static RangedInt GetBotRange(WildSpawnType wildSpawnType) =>
        _botBatteries.GetValueOrDefault(wildSpawnType, _defaultRange);

    public static void DisablePatches() => _patchManager.DisablePatches();

    private static async Task GetConfigFromServerAsync()
    {
        bool error = false;
        try
        {
            string deviceData = await RequestHandler.GetJsonAsync("/BatteriesNotIncluded/GetDeviceData");
            string botBatteries = await RequestHandler.GetJsonAsync("/BatteriesNotIncluded/GetBotBatteries");
            if (string.IsNullOrWhiteSpace(deviceData) || string.IsNullOrWhiteSpace(botBatteries))
            {
                error = true;
            }

            _deviceData = JsonConvert.DeserializeObject<Dictionary<string, DeviceData>>(deviceData!);
            _botBatteries = JsonConvert.DeserializeObject<Dictionary<WildSpawnType, RangedInt>>(botBatteries!);
            if (_deviceData.IsNullOrEmpty() || _botBatteries.IsNullOrEmpty())
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

        LoggerUtil.Info($"Successfully fetched {_deviceData.Count} battery operated devices!");
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
