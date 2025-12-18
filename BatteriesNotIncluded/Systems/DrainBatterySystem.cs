using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Systems;

public class DrainBatterySystem(int runInterval) : BaseDelayedSystem(runInterval)
{
    public override void Run(DeviceManager manager, int i)
    {
        var isActive = manager.IsActive[i];
        if (!isActive) return;

        foreach (var slot in manager.BatterySlots[i])
        {
            if (slot.ContainedItem is not { } item)
            {
                LoggerUtil.Warning($"Tried to drain battery from {slot} while slot.ContainedItem is null");
                continue;
            }
            if (!item.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                LoggerUtil.Warning($"Tried to drain battery from {slot} while ResourceComponent is not found");
                continue;
            }

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
