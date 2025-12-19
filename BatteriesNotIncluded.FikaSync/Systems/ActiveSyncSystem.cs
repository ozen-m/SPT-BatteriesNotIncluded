using System;
using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Systems;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.FikaSync.Systems;

public class ActiveSyncSystem : PacketBaseSystem<DeviceActivePacket>
{
    public override void Run(DeviceManager manager, int i)
    {
        manager.IsActive[i] = CurrentPacket.IsActive;

        var isPrevOperable = manager.IsPrevOperable[i];
        var isOperable = manager.IsOperable[i];

        var component = manager.RelatedComponentRef[i];
        var item = component.Item;
        switch (component)
        {
            case LightComponent lightComponent:
            {
                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) break;

                if (!isOperable && lightComponent.IsActive)
                {
                    // Removed batteries or battery drained
                    lightComponent.IsActive = false;
                    DeviceBridgeSystem.SetLightState(lightComponent);
                }
                // else if (isToggled)
                // {
                //     // Replaced with new batteries while toggled on.
                //     // Different approach when it comes to tac devices (see SetLightsStatePatch)
                //     // since tac devices cannot be "on" while not emitting lights.
                // }

                break;
            }
            case NightVisionComponent nightVisionComponent:
            {
                var isToggled = nightVisionComponent.Togglable.On;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) break;

                // Only control cameras for your player and device is in your equipment
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) break;

                if (!isOperable)
                {
                    // Removed batteries or battery drained
                    CameraUtil.SetNightVision(false);
                }
                else if (isToggled)
                {
                    // Replaced with new batteries while toggled on
                    CameraUtil.SetNightVision(true);
                    playerInvCont.Player_0.PlayNightVisionSound();
                }
                break;
            }
            case ThermalVisionComponent thermalVisionComponent:
            {
                var isToggled = thermalVisionComponent.Togglable.On;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) break;

                // Only control cameras for your player and device is in your equipment
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) break;

                if (!isOperable)
                {
                    // Removed batteries or battery drained
                    CameraUtil.SetThermalVision(false);
                }
                else if (isToggled)
                {
                    // Replaced with new batteries while toggled on
                    CameraUtil.SetThermalVision(true);
                    playerInvCont.Player_0.PlayThermalVisionSound();
                }
                break;
            }
            case TogglableComponent:
            {
                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) break;

                // Only control devices for your player and device is in your equipment
                // (only your player ever hears/sees these)
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If not in active slot (a grid?) */) break;

                switch (item)
                {
                    case SightsItemClass:
                        manager.UpdateSightVisibility(item);
                        break;
                    case HeadphonesItemClass:
                        // NOTE: UpdatePhonesReally runs twice, one on Player.OnItemAddedOrRemoved (too late)
                        //       and on ManualUpdate(Item).
                        playerInvCont.Player_0.UpdatePhonesReally();
                        break;
                }
                break;
            }
            default:
            {
                throw new ArgumentException($"Component {component} is not a valid component");
            }
        }
    }
}
