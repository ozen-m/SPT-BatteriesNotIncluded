using System;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Systems;

public class DrainBatterySystem(int runInterval) : BaseDelayedSystem(runInterval)
{
    /// <summary>
    /// Fika event hook: DeviceIndex, SlotIndex, CurrentCharge
    /// </summary>
    public event Action<int, int, float> OnDrainResource;

    public override void Run(DeviceManager manager, int i)
    {
        var isActive = manager.IsActive[i];
        if (!isActive) return;

        var slots = manager.BatterySlots[i];
        for (var j = 0; j < slots.Length; j++)
        {
            if (slots[j].ContainedItem is not { } item)
            {
                LoggerUtil.Warning($"Tried to drain battery from {slots[j]} while slot.ContainedItem is null");
                continue;
            }
            if (!item.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                LoggerUtil.Warning($"Tried to drain battery from {slots[j]} while ResourceComponent is not found");
                continue;
            }

            // TODO: Drain calculation
            // TODO: Light drain based on modes: light/laser/ir?
            var currentCharge = Mathf.Max(resourceComponent.Value - 50 / 100f * manager.DrainMultiplier[i], 0f);
            resourceComponent.Value = currentCharge;

            // Warning: spams
            LoggerUtil.Debug($"Drained item {manager.Devices[i].LocalizedShortName()} {manager.Devices[i].Id} to {resourceComponent.Value}");
            OnDrainResource?.Invoke(i, j, currentCharge);

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
