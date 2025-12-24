using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceOperableSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        manager.IsPrevOperable[i] = manager.IsOperable[i];

        var slots = manager.BatterySlots[i];
        manager.IsOperable[i] = IsDeviceOperable(slots);
    }

    public static bool IsDeviceOperable(Slot[] slots)
    {
        foreach (var slot in slots)
        {
            var battery = slot.ContainedItem;
            if (battery is null)
            {
                return false;
            }

            if (!battery.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                LoggerUtil.Warning($"Missing resource component for {battery.LocalizedShortName()} ({battery.Id})");
                return false;
            }

            if (resourceComponent.IsDrained())
            {
                return false;
            }
        }
        return true;
    }
}
