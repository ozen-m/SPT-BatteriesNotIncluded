using System.Collections.Generic;
using BatteriesNotIncluded.Components;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SPT.Reflection.Patching;

#pragma warning disable CA2211

namespace BatteriesNotIncluded;

[BepInPlugin("com.ozen.batteriesnotincluded", "Batteries Not Included", "0.0.1")]
public class BatteriesNotIncluded : BaseUnityPlugin
{
    public static ManualLogSource LogSource;
    public static ConfigEntry<bool> DebugLogs;

    private static readonly Dictionary<string, BatteryData> _batteryDataCache = [];

    protected void Awake()
    {
        LogSource = Logger;

        DebugLogs = Config.Bind("Debug", "Logging", true, new ConfigDescription("Show debug logs", null, new ConfigurationManagerAttributes() { Order = 0 }));

        var patchManager = new PatchManager(this, true);
        patchManager.EnablePatches();

        // TODO: Add file check for PrePatch
        // TODO: Check fika compat
    }

    // TODO: Get from server
    public static BatteryData GetBatteryData(string templateId)
    {
        if (_batteryDataCache.TryGetValue(templateId, out var foundTemplate))
        {
            return foundTemplate;
        }

        BatteryData batteryData = new(GetDrainMultiplier(templateId));
        _batteryDataCache.Add(templateId, batteryData);
        return batteryData;
    }

    // Temporary
    private static float GetDrainMultiplier(string templateId)
    {
        // TODO: Implement device specific drain multiplier
        return 1f;
    }
}
