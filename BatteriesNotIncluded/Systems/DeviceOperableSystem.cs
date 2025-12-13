using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceOperableSystem : ISystem
{
    public void Run(DeviceManager manager)
    {
        for (var i = 0; i < manager.Devices.Count; i++)
        {
            Item battery = manager.BatterySlot[i].ContainedItem;
            if (battery is null)
            {
                manager.IsOperable[i] = false;
                manager.ResourceComponentRef[i] = null;
                continue;
            }

            var resourceComponent = battery.GetItemComponent<ResourceComponent>();
            manager.ResourceComponentRef[i] = resourceComponent;
            if (resourceComponent.IsDrained())
            {
                manager.IsOperable[i] = false;
                continue;
            }

            manager.IsOperable[i] = true;
        }
    }
}
