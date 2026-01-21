using System.Collections.Generic;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Patches.Tactical;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;

namespace BatteriesNotIncluded.Systems;

public class DeviceEnforcementSystem : IManualSystem
{
    public void Run(DeviceManager manager, int i)
    {
        var isOperable = manager.IsOperable[i];
        var sameState = manager.IsPrevOperable[i] == isOperable;
        if (sameState) return;

        // Became inoperable/operable
        var component = manager.RelatedComponentRef[i];
        var item = manager.Devices[i];
        switch (component)
        {
            case LightComponent lightComponent:
            {
                if (!isOperable && lightComponent.IsActive)
                {
                    // Removed batteries or battery drained
                    lightComponent.IsActive = false;
                    UpdateLightVisibility(lightComponent, manager);
                }
                /*
                 else if (isToggled)
                {
                    // Replaced with new batteries while toggled on.
                    // Different approach when it comes to tac devices (see SetLightsStatePatch)
                    // since tac devices cannot be "on" while not emitting lights.
                }
                */
                return;
            }
            case NightVisionComponent nightVisionComponent:
            {
                // Only control cameras for your player and device is in your equipment
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) return;

                if (!isOperable)
                {
                    // Removed batteries or battery drained
                    CameraUtil.SetNightVision(false);
                }
                else if (nightVisionComponent.Togglable.On)
                {
                    // Replaced with new batteries while toggled on
                    CameraUtil.SetNightVision(true);
                    playerInvCont.Player_0.PlayNightVisionSound();
                }
                return;
            }
            case ThermalVisionComponent thermalVisionComponent:
            {
                // Only control cameras for your player and device is in your equipment
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) return;

                if (!isOperable)
                {
                    // Removed batteries or battery drained
                    CameraUtil.SetThermalVision(false);
                }
                else if (thermalVisionComponent.Togglable.On)
                {
                    // Replaced with new batteries while toggled on
                    CameraUtil.SetThermalVision(true);
                    playerInvCont.Player_0.PlayThermalVisionSound();
                }
                return;
            }
            case TogglableComponent:
            {
                // Only control devices for your player and device is in your equipment
                // (only your player ever hears/sees these)
                if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
                    !playerInvCont.Player_0.IsYourPlayer ||
                    item.CurrentAddress is GClass3393 /* If not in active slot (a grid?) */) return;

                switch (item)
                {
                    case SightsItemClass:
                        manager.EnforceSightVisibility(item);
                        return;
                    case HeadphonesItemClass:
                        // NOTE: UpdatePhonesReally runs twice, one on Player.OnItemAddedOrRemoved (too late)
                        //       and on ManualUpdate(Item).
                        playerInvCont.Player_0.UpdatePhonesReally();
                        return;
                }
                return;
            }
            default:
            {
                LoggerUtil.Debug($"Component {component} is not a valid component");
                return;
            }
        }
    }

    public static void UpdateLightVisibility(LightComponent lightComponent, DeviceManager manager)
    {
        // Skip patch since we're the one calling
        UpdateBeamsPatch.ToSkip = true;
        try
        {
            if (lightComponent.Item.Owner is not Player.PlayerInventoryController playerInvCont)
            {
                // Fallback to internal UpdateBeams
                manager.UpdateLightVisibility(lightComponent.Item);
                return;
            }

            var player = playerInvCont.Player_0;

            // LightComponent is headlight
            var helmetLights = _helmetLightControllersField(player) as List<TacticalComboVisualController>;
            if (helmetLights?.Count > 0)
            {
                foreach (var lightController in helmetLights)
                {
                    if (lightController.LightMod.Item.Id != lightComponent.Item.Id) continue;

                    UpdateHeadLightState(player);
                    return;
                }
            }

            // LightComponent is weapon light
            if (player.HandsController is Player.FirearmController firearmController)
            {
                firearmController.SetLightsState([lightComponent.GetLightState()], true, false);
                return;
            }

            // Fallback to internal UpdateBeams
            manager.UpdateLightVisibility(lightComponent.Item);
        }
        finally
        {
            UpdateBeamsPatch.ToSkip = false;
        }
    }

    private static void UpdateHeadLightState(Player player)
    {
        // Set animation active to true to avoid animation
        _isHeadLightsAnimationActiveField(player) = true;

        /*
        // IsActive already false;
        FirearmLightStateStruct lightState = lightController.LightMod.GetLightState(true);
        lightController.LightMod.SetLightState(lightState);
        */

        player.SendHeadlightsPacket(false);
        player.SwitchHeadLightsAnimation();
    }

    private static readonly AccessTools.FieldRef<Player, IEnumerable<TacticalComboVisualController>> _helmetLightControllersField =
        AccessTools.FieldRefAccess<Player, IEnumerable<TacticalComboVisualController>>("_helmetLightControllers");

    private static readonly AccessTools.FieldRef<Player, bool> _isHeadLightsAnimationActiveField =
        AccessTools.FieldRefAccess<Player, bool>("IsHeadLightsAnimationActive");
}
