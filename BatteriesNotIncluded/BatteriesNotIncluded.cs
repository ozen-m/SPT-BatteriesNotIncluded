using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Models;
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
    public static ConfigEntry<bool> DebugLogs;

    private static Dictionary<string, DeviceData> _deviceData = [];
    private PatchManager _patchManager;

    protected void Awake()
    {
        LogSource = Logger;

        DebugLogs = Config.Bind("Debug", "Logging", true, new ConfigDescription("Show debug logs", null, new ConfigurationManagerAttributes() { Order = 0 }));

        Fika.IsFikaPresent = Chainloader.PluginInfos.ContainsKey("com.fika.core");

        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();

        _ = Task.Run(() => _ = GetDeviceDataFromServerAsync());

        // TODO: Add file check for PrePatch
    }

    public static bool GetDeviceData(string deviceId, out DeviceData deviceData) =>
        _deviceData.TryGetValue(deviceId, out deviceData);

    private static async Task GetDeviceDataFromServerAsync()
    {
        string errorMsg = "Could not get device data from server. Disabling mod Batteries Not Included";
        bool error = false;
        try
        {
            string json = await RequestHandler.GetJsonAsync("/BatteriesNotIncluded/GetDeviceData");
            if (string.IsNullOrWhiteSpace(json))
            {
                error = true;
            }
            _deviceData = JsonConvert.DeserializeObject<Dictionary<string, DeviceData>>(json!);
            if (_deviceData.IsNullOrEmpty())
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
}
