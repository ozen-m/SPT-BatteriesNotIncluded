using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceOperableSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        if (i == -1) return;

        manager.IsPrevOperable[i] = manager.IsOperable[i];

        var slots = manager.BatterySlots[i];
        for (var j = 0; j < slots.Length; j++)
        {
            var battery = slots[j].ContainedItem;
            if (battery is null)
            {
                manager.IsOperable[i] = false;
                return;
            }

            if (!battery.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                manager.IsOperable[i] = false;
                LoggerUtil.Warning($"Missing resource component for {battery.LocalizedShortName()} ({battery.Id})");
                return;
            }

            if (resourceComponent.IsDrained())
            {
                manager.IsOperable[i] = false;
                return;
            }
        }

        manager.IsOperable[i] = true;
    }
}
