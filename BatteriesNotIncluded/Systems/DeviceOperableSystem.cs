using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceOperableSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        manager.IsPrevOperable[i] = manager.IsOperable[i];

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
    }
}
