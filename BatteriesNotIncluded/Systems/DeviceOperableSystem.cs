using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceOperableSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        if (i == -1) return;

        Item battery = manager.BatterySlot[i].ContainedItem;
        if (battery is null)
        {
            manager.IsOperable[i] = false;
            manager.ResourceComponentRef[i] = null;
            return;
        }

        var resourceComponent = battery.GetItemComponent<ResourceComponent>();
        manager.ResourceComponentRef[i] = resourceComponent;
        if (resourceComponent.IsDrained())
        {
            manager.IsOperable[i] = false;
            return;
        }

        manager.IsOperable[i] = true;
    }
}
