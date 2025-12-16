using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Utils;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;

#pragma warning disable CA2211

namespace BatteriesNotIncluded;

[BepInPlugin("com.ozen.batteriesnotincluded", "Batteries Not Included", "0.0.1")]
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

        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();

        _ = Task.Run(() => _ = GetDeviceDataFromServerAsync());

        // TODO: Add file check for PrePatch
        // TODO: Check fika compat
    }

    public static bool GetDeviceData(string deviceId, out DeviceData deviceData) =>
        _deviceData.TryGetValue(deviceId, out deviceData);

    private async Task GetDeviceDataFromServerAsync()
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
            _patchManager.DisablePatches();
            LoggerUtil.Error(errorMsg);
            return;
        }

        LoggerUtil.Info($"Successfully fetched {_deviceData.Count} battery operated devices!");
    }
}
