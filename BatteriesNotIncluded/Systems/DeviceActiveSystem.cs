using System;
using BatteriesNotIncluded.Managers;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceActiveSystem : IManualSystem
{
    public void Run(DeviceManager manager, int i)
    {
        manager.IsPrevActive[i] = manager.IsActive[i];

        var component = manager.RelatedComponentRef[i];
        manager.IsActive[i] = manager.IsOperable[i] && IsDeviceToggled(component);
    }

    public static bool IsDeviceToggled(GClass3379 component)
    {
        bool isToggled = component switch
        {
            LightComponent lightComponent => lightComponent.IsActive,
            NightVisionComponent nightVisionComponent => nightVisionComponent.Togglable.On,
            ThermalVisionComponent thermalVisionComponent => thermalVisionComponent.Togglable.On,
            TogglableComponent togglableComponent => togglableComponent.On,
            _ => false
        };
        return isToggled;
    }
}
