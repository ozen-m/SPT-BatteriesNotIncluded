using System;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.Systems;

public class DeviceBridgeSystem : ISystem
{
    public void Run(DeviceManager manager)
    {
        for (var i = 0; i < manager.Devices.Count; i++)
        {
            var isOperable = manager.IsOperable[i];
            var previouslyActive = manager.IsActive[i];

            var component = manager.RelatedComponentRef[i];
            switch (component)
            {
                case LightComponent lightComponent:
                {
                    if (!isOperable && previouslyActive)
                    {
                        // Removed batteries or battery drained
                        lightComponent.IsActive = false;
                        SetLightState(lightComponent);
                    }

                    // Replaced with new batteries while toggled on
                    // Different approach when it comes to tac devices

                    manager.IsActive[i] = lightComponent.IsActive;
                    continue;
                }
                case NightVisionComponent nightVisionComponent:
                {
                    var isToggled = nightVisionComponent.Togglable.On;
                    var shouldBeActive = isOperable && isToggled;
                    manager.IsActive[i] = shouldBeActive;

                    if (nightVisionComponent.Item.CurrentAddress is not GClass3393 /* If in active slot (TODO: check if only for your player) */ &&
                        !CameraUtil.NightVisionInProcessSwitching /* Check to not conflict when turning on/off normally */)
                    {
                        if (!shouldBeActive && previouslyActive)
                        {
                            // Removed batteries or battery drained
                            CameraUtil.SetNightVision(false);
                        }
                        else if (shouldBeActive)
                        {
                            // Replaced with new batteries while toggled on
                            CameraUtil.SetNightVision(true);
                            if (nightVisionComponent.Item.Owner is not Player.PlayerInventoryController playerInvCont) continue;

                            playerInvCont.Player_0.PlayNightVisionSound();
                        }
                    }
                    continue;
                }
                case ThermalVisionComponent thermalVisionComponent:
                {
                    var isToggled = thermalVisionComponent.Togglable.On;
                    var shouldBeActive = isOperable && isToggled;
                    manager.IsActive[i] = shouldBeActive;

                    if (thermalVisionComponent.Item.CurrentAddress is not GClass3393 /* If in active slot (TODO: check if only for your player)*/ &&
                        !CameraUtil.ThermalVisionInProcessSwitching /* Check to not conflict when turning on/off normally */)
                    {
                        if (!shouldBeActive && previouslyActive)
                        {
                            // Removed batteries or battery drained
                            CameraUtil.SetThermalVision(false);
                        }
                        else if (shouldBeActive)
                        {
                            // Replaced with new batteries while toggled on
                            CameraUtil.SetThermalVision(true);
                            if (thermalVisionComponent.Item.Owner is Player.PlayerInventoryController playerInvCont)
                            {
                                playerInvCont.Player_0.PlayThermalVisionSound();
                            }
                        }
                    }
                    continue;
                }
                case TogglableComponent togglableComponent:
                {
                    manager.IsActive[i] = isOperable && togglableComponent.On;

                    var item = togglableComponent.Item;
                    if (item.CurrentAddress is not GClass3393 /* If in active slot */)
                    {
                        if (item is SightsItemClass)
                        {
                            manager.UpdateSightVisibility(item.Id, i);
                        }
                        else if (item is HeadphonesItemClass)
                        {
                            if (item.Owner is Player.PlayerInventoryController playerInvCont)
                            {
                                // BUG: UpdatePhonesReally runs twice, one on Player.OnItemAddedOrRemoved (too late)
                                //      and on RunManualUpdateNextFrame.
                                playerInvCont.Player_0.UpdatePhonesReally();
                            }
                        }
                    }
                    continue;
                }
                case null:
                {
                    continue;
                }
                default:
                {
                    throw new ArgumentException($"Component {component} is not a valid component");
                }
            }
        }
    }

    /// <summary>
    /// Thank you IcyClawz for this code.
    /// <br></br><see href="https://github.com/IgorEisberg/SPT-ClientMods/blob/main/ItemContextMenuExt/ItemContextMenuExt.cs#L307"/>
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
