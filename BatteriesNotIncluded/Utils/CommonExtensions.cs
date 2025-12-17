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

        _ = invController.TryRunNetworkTransaction(togglableComponent.Set(true, true));
    }

    /// <summary>
    /// Add battery to slots using DeviceData's batteryId
    /// </summary>
    /// <param name="slots"></param>
    /// <param name="deviceData"></param>
    public static void AddBatteryToSlots(this Slot[] slots, ref DeviceData deviceData)
    {
        foreach (var slot in slots)
        {
            if (slot.ContainedItem is not null) return;

            if (!slot.IsBatterySlot())
            {
                LoggerUtil.Warning($"CommonExtensions::AddBatteryToSlots Cannot add battery to slot, not a battery slot: {slot}");
                return;
            }

            Item battery = Singleton<ItemFactoryClass>.Instance.GetPresetItem(deviceData.Battery);
            if (battery == null || !slot.CheckCompatibility(battery))
            {
                LoggerUtil.Warning($"CommonExtensions::AddBatteryToSlots Slot ({slot}) not compatible with {deviceData.Battery}");
                return;
            }

            slot.Add(battery, false);
        }
    }

    /// <summary>
    /// Drain resource component in slot's contained item, scaled by the player's level.
    /// </summary>
    /// <param name="player">Player to check level for</param>
    public static void DrainResourceComponentInSlots(this Slot[] slots, Player player)
    {
        foreach (var slot in slots)
        {
            var item = slot.ContainedItem;
            if (item is null) continue;

            if (!item.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                LoggerUtil.Warning($"CommonExtensions::DrainResourceComponentInSlots Item does not have a resource component: {item.ToFullString()}");
                continue;
            }

            // TODO: Make configurable
            // Battery charge depends on their max charge and bot level
            var maxValue = (int)resourceComponent.MaxResource;
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
            if (player.AIData?.BotOwner != null && player.AIData.BotOwner.Boss?.IamBoss == true)
            {
                baseValue = maxValue;
            }

            // TODO: Revisit
            var randomCharge = _random.Next(baseValue - 10, baseValue + 5);
            resourceComponent.Value = Mathf.Clamp(randomCharge, 0, maxValue);
            LoggerUtil.Debug($"Set {item.LocalizedShortName()}'s resource value to {randomCharge}");
        }
    }
}
