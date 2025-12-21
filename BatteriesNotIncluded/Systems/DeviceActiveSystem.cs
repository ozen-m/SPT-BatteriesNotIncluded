using System;
using BatteriesNotIncluded.Managers;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceActiveSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        if (i == -1) return;

        manager.IsPrevActive[i] = manager.IsActive[i];

        var component = manager.RelatedComponentRef[i];
        bool isToggled = component switch
        {
            LightComponent lightComponent => lightComponent.IsActive,
            NightVisionComponent nightVisionComponent => nightVisionComponent.Togglable.On,
            ThermalVisionComponent thermalVisionComponent => thermalVisionComponent.Togglable.On,
            TogglableComponent togglableComponent => togglableComponent.On,
            _ => throw new ArgumentException($"Component {component} is not a valid component")
        };

        manager.IsActive[i] = manager.IsOperable[i] && isToggled;
    }
}
