using System;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceBridgeSystem : BaseSystem
{
    public override void Run(DeviceManager manager, int i)
    {
        if (i == -1) return;

        var isPrevOperable = manager.IsPrevOperable[i];
        var isOperable = manager.IsOperable[i];

        var component = manager.RelatedComponentRef[i];
        var item = component.Item;
        switch (component)
        {
            case LightComponent lightComponent:
            {
                var isToggled = lightComponent.IsActive;
                var shouldBeActive = isOperable && isToggled;
                manager.IsActive[i] = shouldBeActive;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) return;

                if (!isOperable)
                {
                    // Removed batteries or battery drained
                    lightComponent.IsActive = false;
                    SetLightState(lightComponent);
                }
                // else if (isToggled)
                // {
                //     // Replaced with new batteries while toggled on.
                //     // Different approach when it comes to tac devices (see SetLightsStatePatch)
                //     // since tac devices cannot be "on" while not emitting lights.
                // }

                return;
            }
            case NightVisionComponent nightVisionComponent:
            {
                var isToggled = nightVisionComponent.Togglable.On;
                manager.IsActive[i] = isOperable && isToggled;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) return;

                // Only control cameras for your player and device is in your equipment
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) return;

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
                return;
            }
            case ThermalVisionComponent thermalVisionComponent:
            {
                var isToggled = thermalVisionComponent.Togglable.On;
                manager.IsActive[i] = isOperable && isToggled;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) return;

                // Only control cameras for your player and device is in your equipment
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) return;

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
                return;
            }
            case TogglableComponent togglableComponent:
            {
                var isToggled = togglableComponent.On;
                manager.IsActive[i] = isOperable && isToggled;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) return;

                // Only control devices for your player and device is in your equipment
                // (only your player ever hears/sees these)
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If not in active slot (a grid?) */) return;

                switch (item)
                {
                    case SightsItemClass:
                        manager.UpdateSightVisibility(item);
                        break;
                    case HeadphonesItemClass:
                        // BUG: UpdatePhonesReally runs twice, one on Player.OnItemAddedOrRemoved (too late)
                        //      and on ManualUpdate(Item).
                        playerInvCont.Player_0.UpdatePhonesReally();
                        break;
                }
                return;
            }
            default:
            {
                throw new ArgumentException($"Component {component} is not a valid component");
            }
        }
    }

    /// <summary>
    /// Thank you IcyClawz for this code.
    /// <br></br><see href="https://github.com/IgorEisberg/SPT-ClientMods/blob/main/ItemContextMenuExt/ItemContextMenuExt.cs#L307"/>
    /// <br></br><see href="https://github.com/IgorEisberg/SPT-ClientMods/blob/main/LICENSE"/>
    /// </summary>
    private static void SetLightState(LightComponent lightComponent)
    {
        if (lightComponent.Item.Owner is not Player.PlayerInventoryController playerInvCont)
        {
            LoggerUtil.Debug($"Could not find player when turning off light for item ${lightComponent.Item.LocalizedShortName()} {lightComponent.Item.Id}");
            return;
        }

        var player = playerInvCont.Player_0;
        if (player.HandsController is Player.FirearmController firearmController)
        {
            firearmController.SetLightsState([lightComponent.GetLightState()], true, false);
        }
        else
        {
            // TODO: Tac devices on the ground do not turn off
            foreach (TacticalComboVisualController lightController in player.GetComponentsInChildren<TacticalComboVisualController>())
            {
                if (!ReferenceEquals(lightController.LightMod, lightComponent)) continue;

                lightController.UpdateBeams();
                LoggerUtil.Debug($"DeviceBridgeSystem::SetLightState lightController.UpdateBeams");
            }
        }
    }
}
