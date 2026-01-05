using System.Reflection;
using BatteriesNotIncluded.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

#pragma warning disable CS0618 // Type or member is obsolete

namespace BatteriesNotIncluded.Utils;

[Injectable(InjectionType.Singleton)]
public class ConfigUtil
{
    public ModConfig ModConfig { get; }

    public string ConfigPath { get; }

    public ConfigUtil(ISptLogger<BatteriesNotIncluded> logger, ModHelper modHelper)
    {
        string modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        ConfigPath = Path.Combine(modPath, "config");

        try
        {
            ModConfig = modHelper.GetJsonDataFromFile<ModConfig>(ConfigPath, "config.jsonc");
            ModConfig.GlobalDrainMult = Math.Max(ModConfig.GlobalDrainMult, double.Epsilon);
        }
        catch (Exception ex)
        {
            ModConfig = new ModConfig();
            logger.Error(ex.ToString());
            logger.Error("[Batteries Not Included] Exception while trying to load configuration file, disabling mod.");
        }

        try
        {
            if (!ModConfig.Enabled) return;

            var customDevices = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Dictionary<MongoId, DeviceData>>>(ConfigPath, "customDevices.jsonc");
            foreach (var (batteryId, deviceDatas) in customDevices)
            {
                foreach (var (deviceId, deviceData) in deviceDatas)
                {
                    ModConfig.DeviceBatteryData[batteryId][deviceId] = deviceData;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex.Message);
            logger.Warning("[Batteries Not Included] Exception while trying to load mod compatibility file.");
        }

        ConvertTacticalDevicesDrain();
        ProcessAllDeviceData();
        ModConfig.DeviceBatteryData = null;
    }

    /// <summary>
    /// Convert TacticalDevicesDrain real runtime hours from config to game runtime seconds for client use
    /// </summary>
    private void ConvertTacticalDevicesDrain()
    {
        foreach (var (mode, hours) in ModConfig.TacticalDevicesDrain)
        {
            var seconds = RuntimeToSeconds(hours);
            ModConfig.TacticalDevicesDrain[mode] = 100d / seconds;
        }
    }

    /// <summary>
    /// Fill in DeviceData properties and add to DeviceDefinitions
    /// </summary>
    private void ProcessAllDeviceData()
    {
        foreach (var (batteryType, deviceDatas) in ModConfig.DeviceBatteryData)
        {
            foreach ((MongoId deviceId, DeviceData deviceData) in deviceDatas)
            {
                const double maxResourceValue = 100f; // Ideally should come from the battery template
                var gameRuntimeSecs = RuntimeToSeconds(deviceData.RealRuntimeHr) / ModConfig.GlobalDrainMult;

                deviceData.Battery = batteryType;
                deviceData.GameRuntimeSecs = gameRuntimeSecs;
                deviceData.DrainPerSecond = maxResourceValue / gameRuntimeSecs;

                ModConfig.DeviceBatteryDefinitions[deviceId] = deviceData;
            }
        }
    }

    private static readonly double _logMin = Math.Log10(1d);
    private static readonly double _logRange = Math.Log10(100_000d) - _logMin;

    /// <summary>
    /// Normalized log interpolation.
    /// Map minimum: 1 hour runtime to <see cref="ModConfig.MinGameRuntime"/>;
    ///     maximum: 100,000 hours runtime to <see cref="ModConfig.MaxGameRuntime"/>.
    /// </summary>
    /// <param name="runtimeHours">Device battery life in hours</param>
    /// <returns>Real runtime hours mapped to game seconds</returns>
    private double RuntimeToSeconds(double runtimeHours)
    {
        runtimeHours = Math.Clamp(runtimeHours, 1d, 100_000d);
        double tMin = ModConfig.MinGameRuntime;
        double tMax = ModConfig.MaxGameRuntime;

        double factor = (Math.Log10(runtimeHours) - _logMin) / _logRange;
        return (int)(tMin + (tMax - tMin) * factor);
    }
}
