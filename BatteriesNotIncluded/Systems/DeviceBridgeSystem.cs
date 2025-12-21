using System;
using System.Collections.Generic;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Patches.Tactical;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;

namespace BatteriesNotIncluded.Systems;

public class DeviceBridgeSystem : BaseSystem
{
    /// <summary>
    /// Fika event hook: DeviceId, IsActive
    /// </summary>
    public event Action<string, bool> OnSetDeviceActive;

    public override void Run(DeviceManager manager, int i)
    {
        if (i == -1) return;

        var isPrevOperable = manager.IsPrevOperable[i];
        var isOperable = manager.IsOperable[i];
        bool isActive;

        var component = manager.RelatedComponentRef[i];
        var item = component.Item;
        switch (component)
        {
            case LightComponent lightComponent:
            {
                var isToggled = lightComponent.IsActive;
                isActive = isOperable && isToggled;

                // Became inoperable/operable
                var shouldTransition = isPrevOperable ^ isOperable;
                if (!shouldTransition) break;

                if (!isOperable && lightComponent.IsActive)
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

                break;
            }
            case NightVisionComponent nightVisionComponent:
            {
                var isToggled = nightVisionComponent.Togglable.On;
                isActive = isOperable && isToggled;

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
                isActive = isOperable && isToggled;

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
            case TogglableComponent togglableComponent:
            {
                var isToggled = togglableComponent.On;
                isActive = isOperable && isToggled;

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

        manager.IsActive[i] = isActive;
        OnSetDeviceActive?.Invoke(item.Id, isActive);
    }

    /// <summary>
    /// Thank you IcyClawz for this code.
    /// <br></br><see href="https://github.com/IgorEisberg/SPT-ClientMods/blob/main/ItemContextMenuExt/ItemContextMenuExt.cs#L307"/>
    /// <br></br><see href="https://github.com/IgorEisberg/SPT-ClientMods/blob/main/LICENSE"/>
    /// </summary>
    public static void SetLightState(LightComponent lightComponent)
    {
        // Skip patch since we're the one calling
        UpdateBeamsPatch.ToSkip = true;
        try
        {
            if (lightComponent.Item.Owner is not Player.PlayerInventoryController playerInvCont)
            {
                // TODO: Dead bots inventory, see ItemContextExtended
                LoggerUtil.Debug($"Could not find player when turning off light for item {lightComponent.Item.LocalizedShortName()} {lightComponent.Item.Id}");
                return;
            }

            var player = playerInvCont.Player_0;

            // LightComponent is headlight
            var helmetLights = _helmetLightControllersField(player) as List<TacticalComboVisualController>;
            if (helmetLights!.Count > 0)
            {
                foreach (var lightController in helmetLights)
                {
                    if (lightController.LightMod.Item.Id != lightComponent.Item.Id) continue;

                    SetHeadLightState(player);
                    return;
                }
            }

            // LightComponent is weapon light
            if (player.HandsController is Player.FirearmController firearmController)
            {
                firearmController.SetLightsState([lightComponent.GetLightState()], true, false);
            }
        }
        finally
        {
            UpdateBeamsPatch.ToSkip = false;
        }
    }

    private static void SetHeadLightState(Player player)
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
