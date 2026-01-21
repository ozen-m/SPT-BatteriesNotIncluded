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
    private static readonly MongoID _aaBatteryId = "5672cb124bdc2d1a0f8b4568";
    private static readonly MongoID _cr2032BatteryId = "5672cb304bdc2dc2088b456a";
    private static readonly MongoID _cr123ABatteryId = "590a358486f77429692b2790";

    private static readonly HashSet<MongoID> _batteryIds = [_aaBatteryId, _cr2032BatteryId, _cr123ABatteryId];
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

    public static Slot[] GetBatterySlots(this CompoundItem device, int? slotCount = null)
    {
        if (slotCount is null)
        {
            slotCount = 0;
            foreach (var slot in device.Slots)
            {
                if (!slot.IsBatterySlot()) continue;
        
                slotCount++;
            }
        }

        var batterySlots = new Slot[slotCount.Value];
        var slotIndex = 0;
        foreach (var slot in device.Slots)
        {
            if (!slot.IsBatterySlot()) continue;

            batterySlots[slotIndex++] = slot;
        }
#if DEBUG
        foreach (var slot in batterySlots)
        {
            if (slot is null) LoggerUtil.Warning($"Cannot get all battery slots ({slotCount}) for {device.LocalizedShortName()} ({device.Id})");
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
    public static void PlayToggleSound(this Player player, AudioClip soundToPlay, Vector3 speechLocalPosition) =>
        Singleton<BetterAudio>.Instance.PlayAtPoint(
            player.Transform.Original.position + speechLocalPosition,
            soundToPlay,
            player.Distance,
            BetterAudio.AudioSourceGroupType.Character,
            5
        );

    public static void ToggleItem(this Item item, bool? isActive = null)
    {
        if (item.Owner is not TraderControllerClass invController) return;
        if (!item.TryGetItemComponent(out TogglableComponent togglableComponent)) return;
        if (togglableComponent.On == isActive) return;

        isActive ??= !togglableComponent.On;
        var toggleOperation = togglableComponent.Set(isActive.Value, true);
        if (toggleOperation.Failed)
        {
            LoggerUtil.Warning($"Failed to toggle {item.ToFullString()}: {toggleOperation.Error}");
            return;
        }

        invController.RunNetworkTransaction(toggleOperation.Value);
    }

    /// <summary>
    /// Add battery to slot using DeviceData's batteryId
    /// </summary>
    /// <param name="slots"></param>
    /// <param name="deviceData"></param>
    public static Item CreateBatteryForSlot(this Slot slot, ref DeviceData deviceData)
    {
        if (!slot.IsBatterySlot())
        {
            LoggerUtil.Warning($"GetBatteriesForSlots: Cannot add battery to slot, not a battery slot: {slot}");
            return null;
        }

        if (slot.ContainedItem is not null && slot.ContainedItem.IsBattery())
        {
            return slot.ContainedItem;
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

        int baseValue;
        var maxValue = (int)resourceComponent.MaxResource;
        float levelFactor = Mathf.Clamp01(player.Profile.Info.Level / 42f /* Highest trader level requirement */);

        // Bosses will get almost full battery
        if (player.Profile.Info.Settings.Role.IsABossOrFollower())
        {
            baseValue = maxValue;
        }
        else if (player.Side is not EPlayerSide.Savage)
        {
            // Pmc, use player's level to determine battery charge
            var range = BatteriesNotIncluded.GetBotRange(WildSpawnType.pmcBot);
            baseValue = (int)Mathf.Lerp(range.Min, range.Max, levelFactor);
        }
        else
        {
            // Scav
            var range = BatteriesNotIncluded.GetBotRange(WildSpawnType.assault);
            baseValue = (int)Mathf.Lerp(range.Min, range.Max, levelFactor);
        }

        var lowerLimit = baseValue - 10;
        var upperLimit = baseValue + 5;

        var randomCharge = _random.Next(lowerLimit, upperLimit);
        const int minValue = 10; // Give them at least 10 charge
        resourceComponent.Value = Mathf.Clamp(randomCharge, minValue, maxValue);
        LoggerUtil.Debug($"Set battery {item} charge to {resourceComponent.Value}");
    }

    public static bool AddBatteryToSlot(this Slot slot, Item battery)
    {
        if (slot is null || slot.ContainedItem is not null || battery is null) return false;

        var addOp = slot.Add(battery, false);
        if (addOp is { Failed: true, Error: not Slot.GClass1578 } /* Inventory Errors/Slot not empty */)
        {
            LoggerUtil.Warning($"Failed to add {battery} to {slot}: {addOp.Error}");
            return false;
        }

        return true;
    }

    private static bool IsABossOrFollower(this WildSpawnType role)
    {
        return role.IsBossOrFollower() && (role is not (WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC or WildSpawnType.pmcBot or WildSpawnType.assaultGroup));
    }
}
