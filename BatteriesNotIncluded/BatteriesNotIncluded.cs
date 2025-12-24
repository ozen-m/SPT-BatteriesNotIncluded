using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Patches;
using BatteriesNotIncluded.Patches.Earpiece;
using BatteriesNotIncluded.Patches.Headwear;
using BatteriesNotIncluded.Patches.LifeCycle;
using BatteriesNotIncluded.Patches.Sight;
using BatteriesNotIncluded.Patches.Tactical;
using BatteriesNotIncluded.Patches.Tactical.Bot;
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

[BepInPlugin("com.ozen.batteriesnotincluded", "Batteries Not Included", "0.0.1")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
public class BatteriesNotIncluded : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static ConfigEntry<bool> ShowRemainingBattery;
    public static ConfigEntry<bool> DebugLogs;

    private static Dictionary<string, DeviceData> _deviceData = [];
    private static Dictionary<WildSpawnType, RangedInt> _botBatteries = [];

    private PatchManager _patchManager;

    protected void Awake()
    {
        LogSource = Logger;

        CheckForPrepatch();

        ShowRemainingBattery = Config.Bind("General", "Remaining Battery Tooltip", true, new ConfigDescription("Show remaining runtime when hovering over a device", null, new ConfigurationManagerAttributes() { Order = 1 }));
        DebugLogs = Config.Bind("Debug", "Logging", true /* TODO: disable on release */, new ConfigDescription("Show debug logs", null, new ConfigurationManagerAttributes() { IsAdvanced = true, Order = 0 }));

        Fika.IsFikaPresent = Chainloader.PluginInfos.ContainsKey("com.fika.core");

        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();

        _ = Task.Run(() => _ = GetConfigFromServerAsync());
    }

    public static bool GetDeviceData(string deviceId, out DeviceData deviceData) =>
        _deviceData.TryGetValue(deviceId, out deviceData);

    public static RangedInt GetBotRange(WildSpawnType wildSpawnType) =>
        _botBatteries.GetValueOrDefault(wildSpawnType, _defaultRange);

    private static async Task GetConfigFromServerAsync()
    {
        string errorMsg = "Could not get configuration from the server. Disabling mod Batteries Not Included";
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
        catch (Exception ex)
        {
            errorMsg = $"{ex}\n{errorMsg}";
            error = true;
        }

        if (error)
        {
            // _patchManager.DisablePatches(); // SPT Bug?
            DisablePatches();
            LoggerUtil.Error(errorMsg);
            return;
        }

        LoggerUtil.Info($"Successfully fetched {_deviceData.Count} battery operated devices!");
    }

    public static void DisablePatches()
    {
        new TooltipPatch().Disable();
        new GetHeadLightStatePatch().Disable();
        new SetLightsStatePatch().Disable();
        new UpdateBeamsPatch().Disable();
        new TurnOnPatch().Disable();
        new CaptureSightControllerPatch().Disable();
        new SightsChangePatch().Disable();
        // new SightsItemCtorPatch().Disable();
        new GameWorldCreatePatch().Disable();
        new TogglableConflictPatch().Disable();
        new NightVisionOnPatch().Disable();
        new ThermalVisionOnPatch().Disable();
        new PlayNightVisionSoundPatch().Disable();
        new PlayThermalVisionSoundPatch().Disable();
        // new HeadphonesCtorPatchPatch().Disable();
        new HeadphoneTemplatePatch().Disable();
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
        try
        {
            SightsTogglableField = AccessTools.FieldRefAccess<SightsItemClass, TogglableComponent>("Togglable");
        }
        catch
        {
            // Ignored
        }
    }

    public static AccessTools.FieldRef<HeadphonesItemClass, TogglableComponent> HeadphonesTogglableField;
    public static AccessTools.FieldRef<SightsItemClass, TogglableComponent> SightsTogglableField;
    private static readonly RangedInt _defaultRange = new(40, 60);
}
