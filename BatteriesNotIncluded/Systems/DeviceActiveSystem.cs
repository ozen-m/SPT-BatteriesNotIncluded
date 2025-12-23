using System;
using BatteriesNotIncluded.Managers;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceActiveSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        manager.IsPrevActive[i] = manager.IsActive[i];

        var component = manager.RelatedComponentRef[i];
        manager.IsActive[i] = manager.IsOperable[i] && GetIsToggled(component);
    }

    public static bool GetIsToggled(GClass3379 component)
    {
        bool isToggled = component switch
        {
            LightComponent lightComponent => lightComponent.IsActive,
            NightVisionComponent nightVisionComponent => nightVisionComponent.Togglable.On,
            ThermalVisionComponent thermalVisionComponent => thermalVisionComponent.Togglable.On,
            TogglableComponent togglableComponent => togglableComponent.On,
            _ => throw new ArgumentException($"Component {component} is not a valid component")
        };
        return isToggled;
    }
}
