using System;
using System.Collections.Generic;
using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace BatteriesNotIncluded.Managers;

public class TacticalVisualHandler(DeviceManager manager)
{
    private readonly HashSet<TacticalComboVisualController> _controllers = [];

    public void UpdateDeviceMode(TacticalComboVisualController controller)
    {
        var lightComponent = controller.LightMod;
        var item = lightComponent.Item;
        var index = manager.GetItemIndex(item);
        if (index == -1) return;

        _controllers.Add(controller);
        if (!controller.gameObject.activeInHierarchy) return;
        if (!lightComponent.IsActive) return;

        // Thank you SAIN!
        var deviceMode = DeviceMode.None;
        foreach (var mode in _tacticalModesField(controller))
        {
            if (!mode.gameObject.activeInHierarchy) continue;

            deviceMode = BatteriesNotIncluded.GetDeviceModeOverride(item.TemplateId, mode.name);
            if (deviceMode is not DeviceMode.None) break;

            for (var i = 0; i < mode.childCount; i++)
            {
                var light = mode.GetChild(i);
                var lightName = light.name;

                deviceMode |= lightName switch
                {
                    not null when lightName.StartsWith("light_", StringComparison.OrdinalIgnoreCase) => DeviceMode.Flashlight,
                    not null when lightName.StartsWith("vis_", StringComparison.OrdinalIgnoreCase) => DeviceMode.VisibleLaser,
                    not null when lightName.StartsWith("il_", StringComparison.OrdinalIgnoreCase) => DeviceMode.IRFlood,
                    not null when lightName.StartsWith("ir_", StringComparison.OrdinalIgnoreCase) => DeviceMode.IRLaser,
                    _ => 0
                };
            }
            break;
        }
        if (deviceMode is DeviceMode.None)
        {
            LoggerUtil.Warning($"DeviceMode is `None` for item: {item.ToFullString()}");
        }

        UpdateDeviceDrainPerSecond(index, deviceMode);
#if DEBUG
        LoggerUtil.Info($"{item.LocalizedShortName()} ({item.StringTemplateId}): {deviceMode.ToString()}");
#endif
    }

    public void TurnOffLightVisibility(Item item)
    {
        foreach (var controller in _controllers)
        {
            if (controller.LightMod.Item != item) continue;

            controller.LightMod.IsActive = false;
            var isYourPlayer = controller.LightMod.Item.Owner is Player.PlayerInventoryController inv && inv.Player_0.IsYourPlayer;

            controller.UpdateBeams(isYourPlayer);
        }
    }

    /// <summary>
    /// Remove null controllers, used OnGameStart.
    /// I'm assuming these are templates of controllers added before the start of the game.
    /// </summary>
    public void RemoveDestroyedControllers()
    {
        var num = _controllers.RemoveWhere((c) => c == null);
        LoggerUtil.Debug($"Removed {num} null tactical controllers");
    }

    private void UpdateDeviceDrainPerSecond(int index, DeviceMode deviceMode)
    {
        var newDrain = BatteriesNotIncluded.GetTacticalDrain(deviceMode);
        manager.DrainPerSecond[index] = newDrain;
    }

    private static readonly AccessTools.FieldRef<TacticalComboVisualController, List<Transform>> _tacticalModesField =
        AccessTools.FieldRefAccess<TacticalComboVisualController, List<Transform>>("list_0");
}
