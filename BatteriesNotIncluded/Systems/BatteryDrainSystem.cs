using System;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Systems;

public class BatteryDrainSystem(float runInterval) : BaseDelayedSystem(runInterval)
{
    /// <summary>
    /// Fika event hook: DeviceIndex, SlotIndex, CurrentCharge
    /// </summary>
    public event Action<string, int, float> OnDrainResource;

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

            var currentCharge = Mathf.Max(resourceComponent.Value - (RunInterval / 1000f * manager.DrainPerSecond[i]), 0f);
            resourceComponent.Value = currentCharge;

            // Probably not needed, but nice to have
            // item.RaiseRefreshEvent(false, false); 

#if DEBUG
            // Warning: spams
            LoggerUtil.Debug($"Drained item {manager.Devices[i].LocalizedShortName()} {manager.Devices[i].Id} to {resourceComponent.Value}");
#endif

            OnDrainResource?.Invoke(manager.Devices[i].Id, j, currentCharge);

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
