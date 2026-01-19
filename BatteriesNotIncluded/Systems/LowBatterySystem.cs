using System;
using System.Collections;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Systems;

public class LowBatterySystem(float runInterval) : BaseDelayedSystem(runInterval)
{
    public override void Run(DeviceManager manager, int i)
    {
        var isActive = manager.IsActive[i];
        if (!isActive) return;

        // Only warn for your player and device is in your equipment
        var item = manager.Devices[i];
        if (item.Owner is not Player.PlayerInventoryController playerInvCont ||
            !playerInvCont.Player_0.IsYourPlayer ||
            item.CurrentAddress is GClass3393 /* If in active slot (a grid?) */) return;

        if (IsLowBattery(manager, i))
        {
            manager.StartCoroutine(ShowWarning(manager, i, 3));
        }
    }

    private bool IsLowBattery(DeviceManager manager, int i)
    {
        var minResourceVal = float.MaxValue;
        foreach (var slot in manager.BatterySlots[i])
        {
            if (slot.ContainedItem is null || !slot.ContainedItem.TryGetItemComponent(out ResourceComponent resource))
            {
                LoggerUtil.Warning($"{nameof(LowBatterySystem)}::{nameof(IsLowBattery)} Slot has missing batteries while active for device {manager.Devices[i].ToFullString()}");
                continue;
            }

            var val = resource.Value;
            if (val < minResourceVal)
            {
                minResourceVal = val;
            }
        }

        var runtimeMins = minResourceVal / manager.DrainPerSecond[i] / 60f;
        return runtimeMins < 5f; // TODO: add config
    }

    private static readonly WaitForSeconds _waitInterval = new(0.3f);

    private static IEnumerator ShowWarning(DeviceManager manager, int i, int count)
    {
        var component = manager.RelatedComponentRef[i];
        var item = manager.Devices[i];
        switch (component)
        {
            case LightComponent lightComponent:
            {
                for (int j = 0; j < count; j++)
                {
                    lightComponent.IsActive = false;
                    DeviceEnforcementSystem.UpdateLightVisibility(lightComponent, manager);
                    yield return _waitInterval;

                    lightComponent.IsActive = true;
                    DeviceEnforcementSystem.UpdateLightVisibility(lightComponent, manager);
                    yield return _waitInterval;
                }

                yield break;
            }
            case NightVisionComponent:
            case ThermalVisionComponent:
            {
                NotificationManagerClass.DisplayWarningNotification(
                    $"Low Battery Warning: {item.LocalizedName()}",
                    ENotificationDurationType.Long
                );

                yield break;
            }
            case TogglableComponent:
            {
                switch (item)
                {
                    case SightsItemClass:
                        for (int j = 0; j < count; j++)
                        {
                            manager.UpdateSightVisibility(item, false);
                            yield return _waitInterval;

                            manager.UpdateSightVisibility(item, true);
                            yield return _waitInterval;
                        }
                        break;
                    case HeadphonesItemClass:
                        NotificationManagerClass.DisplayWarningNotification(
                            $"Low Battery: {item.LocalizedName()}",
                            ENotificationDurationType.Long
                        );
                        break;
                }

                yield break;
            }
            default:
            {
                throw new ArgumentException($"Component {component.GetType()} is not a valid component");
            }
        }
    }
}
