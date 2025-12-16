using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using UnityEngine;

namespace BatteriesNotIncluded.Systems;

public class DrainBatterySystem(int runInterval) : BaseDelayedSystem(runInterval)
{
    public override void Run(DeviceManager manager, int i)
    {
        var isActive = manager.IsActive[i];
        if (!isActive) return;

        var resourceComponents = manager.ResourceComponentRef[i];
        foreach (var resourceComponent in resourceComponents)
        {
            // TODO: Drain calculation
            // TODO: Light drain based on modes: light/laser/ir?
            var currentCharge = Mathf.Max(resourceComponent.Value - 50 / 100f * manager.DrainMultiplier[i], 0f);
            resourceComponent.Value = currentCharge;

            // Warning: spams
            LoggerUtil.Debug($"Drained item {manager.Devices[i].LocalizedShortName()} {manager.Devices[i].Id} to {resourceComponent.Value}");

            if (currentCharge == 0f)
            {
                manager.ManualUpdate(manager.Devices[i].Id);
            }
        }
    }

    public override void ForceRun(DeviceManager manager)
    {
    }
}
