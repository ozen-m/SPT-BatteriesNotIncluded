using System;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceOperableSystem : BaseSystem
{
    /// <summary>
    /// Fika event hook: DeviceIndex, IsPrevOperable, IsOperable 
    /// </summary>
    public event Action<int, bool, bool> OnSetDeviceOperable;

    public override void Run(DeviceManager manager, int i)
    {
        if (i == -1) return;

        var isPrevOperable = manager.IsOperable[i];
        manager.IsPrevOperable[i] = isPrevOperable;

        var isOperable = true;
        var slots = manager.BatterySlots[i];
        foreach (var slot in slots)
        {
            var battery = slot.ContainedItem;
            if (battery is null)
            {
                isOperable = false;
                break;
            }

            if (!battery.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                isOperable = false;
                LoggerUtil.Warning($"Missing resource component for {battery.LocalizedShortName()} ({battery.Id})");
                break;
            }

            if (resourceComponent.IsDrained())
            {
                isOperable = false;
                break;
            }
        }

        manager.IsOperable[i] = isOperable;
        OnSetDeviceOperable?.Invoke(i, isPrevOperable, isOperable);
    }
}
