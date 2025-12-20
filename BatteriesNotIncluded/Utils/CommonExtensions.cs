using System;
using System.Collections.Generic;
using BatteriesNotIncluded.Models;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using Random = System.Random;

namespace BatteriesNotIncluded.Utils;

public static class CommonExtensions
{
    private const string AABatteryId = "5672cb124bdc2d1a0f8b4568";
    private const string CR2032BatteryId = "5672cb304bdc2dc2088b456a";
    private const string CR123ABatteryId = "590a358486f77429692b2790";

    private static readonly HashSet<MongoID> _batteryIds = [AABatteryId, CR2032BatteryId, CR123ABatteryId];
    private static readonly Random _random = new();

    public static bool IsBatteryOperated(this Item item) => item is CompoundItem compoundItem && compoundItem.IsBatteryOperated();

    public static bool IsBatteryOperated(this CompoundItem item)
    {
        foreach (var slot in item.Slots)
        {
            if (slot.IsBatterySlot())
            {
                return true;
            }
        }
        return false;
    }

    public static Slot[] GetBatterySlots(this CompoundItem device, int slotCount)
    {
        var batterySlots = new Slot[slotCount];
        var slotIndex = 0;
        foreach (var slot in device.Slots)
        {
            if (!slot.IsBatterySlot()) continue;

            batterySlots[slotIndex++] = slot;
        }
#if DEBUG
        foreach (var slot in batterySlots)
        {
            if (slot is null) throw new InvalidOperationException($"Cannot get all battery slots ({slotCount}) for {device.LocalizedShortName()} ({device.Id})");
        }
#endif
        return batterySlots;
    }

    public static bool IsBattery(this Item item) => _batteryIds.Contains(item.TemplateId);

    public static bool IsBatterySlot(this Slot slot)
    {
        foreach (var slotFilter in slot.Filters)
        {
            foreach (var filter in slotFilter.Filter)
            {
                if (_batteryIds.Contains(filter))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsDrained(this ResourceComponent component) => component.Value <= 0f;

    /// <summary>
    /// Player.PlayToggleSound not relying on previous state
    /// </summary>
    public static void PlayToggleSound(this Player player, AudioClip soundToPlay, Vector3 speechLocalPosition)
    {
        Singleton<BetterAudio>.Instance.PlayAtPoint(player.Transform.Original.position + speechLocalPosition, soundToPlay, player.Distance, BetterAudio.AudioSourceGroupType.Character, 5);
    }

    public static void TurnOnDevice(this Item item)
    {
        if (!item.TryGetItemComponent(out TogglableComponent togglableComponent)) return;
        if (togglableComponent.Item.Owner is not InventoryController invController) return;
        if (togglableComponent.On) return;

        var toggleOperation = togglableComponent.Set(true, true);
        if (toggleOperation.Failed)
        {
            LoggerUtil.Warning($"Failed to toggle device: {item}");
            return;
        }

        invController.RunNetworkTransaction(toggleOperation.Value);
    }

    /// <summary>
    /// Add battery to slots using DeviceData's batteryId
    /// </summary>
    /// <param name="slots"></param>
    /// <param name="deviceData"></param>
    public static Item CreateBatteryForSlot(this Slot slot, ref DeviceData deviceData)
    {
        if (slot.ContainedItem is not null) return null;

        if (!slot.IsBatterySlot())
        {
            LoggerUtil.Warning($"GetBatteriesForSlots: Cannot add battery to slot, not a battery slot: {slot}");
            return null;
        }

        Item battery = Singleton<ItemFactoryClass>.Instance.GetPresetItem(deviceData.Battery);
        if (slot.CheckCompatibility(battery)) return battery;

        LoggerUtil.Warning($"GetBatteriesForSlots: Slot ({slot}) not compatible with {deviceData.Battery}");
        return null;
    }

    /// <summary>
    /// Drain resource component in slot's contained item, scaled by the player's level.
    /// </summary>
    /// <param name="player">Player to check level for</param>
    public static void DrainBattery(this Item item, Player player)
    {
        if (item is null) return;

        if (!item.TryGetItemComponent(out ResourceComponent resourceComponent))
        {
            LoggerUtil.Warning($"Item does not have a resource component: {item.ToFullString()}");
            return;
        }

        int maxValue = 100;
        int baseValue;
        float levelFactor = Mathf.Clamp01(player.Profile.Info.Level / 42f /* Highest trader level requirement */);

        if (player.Side is not EPlayerSide.Savage)
        {
            // Use player level to determine battery charge
            baseValue = (int)(Mathf.Lerp(50f, maxValue, levelFactor));
        }
        else
        {
            // Scav
            baseValue = (int)(Mathf.Lerp(20, 60f, levelFactor));
        }

        // Boss almost full battery
        // BUG: Player.AIData is empty? See Player.set_AIData
        if (player.Profile.Info.Settings.Role.IsABossOrFollower())
        {
            baseValue = maxValue;
        }

        var lowerLimit = baseValue - 10;
        var upperLimit = baseValue + 5;

        // TODO: Make configurable
        // Battery charge depends on their max charge and bot level
        maxValue = (int)resourceComponent.MaxResource;

        // TODO: Revisit
        var randomCharge = _random.Next(lowerLimit, upperLimit);
        resourceComponent.Value = Mathf.Clamp(randomCharge, 0, maxValue);
        LoggerUtil.Debug($"Set {item.LocalizedShortName()} ({item.Id}) resource component value to {randomCharge}");
    }

    public static bool AddBatteryToSlot(this Slot slot, Item battery)
    {
        if (slot is null || slot.ContainedItem is not null || battery is null) return false;

        var addOp = slot.Add(battery, false);
        if (addOp.Failed)
        {
            LoggerUtil.Warning($"Failed to add {battery} to {slot}: {addOp.Error}");
        }

        return true;
    }

    public static bool IsABossOrFollower(this WildSpawnType role)
    {
        return role.IsBossOrFollower() && (role is not (WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC or WildSpawnType.pmcBot or WildSpawnType.assaultGroup));
    }
}
