using System;
using System.Collections.Generic;
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

    private static Item _aaBatteryTemplate;
    private static Item _cr2032BatteryTemplate;
    private static Item _cr123ABatteryTemplate;

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

        if (togglableComponent.Item.Owner is InventoryController invController)
        {
            _ = invController.TryRunNetworkTransaction(togglableComponent.Set(true, true));
        }
    }

    public static void AddBatteriesToBotDevice(this Player botPlayer, Slot[] slots)
    {
        _aaBatteryTemplate ??= Singleton<ItemFactoryClass>.Instance.GetPresetItem(AABatteryId);
        _cr2032BatteryTemplate ??= Singleton<ItemFactoryClass>.Instance.GetPresetItem(CR2032BatteryId);
        _cr123ABatteryTemplate ??= Singleton<ItemFactoryClass>.Instance.GetPresetItem(CR123ABatteryId);

        foreach (var slot in slots)
        {
            if (slot.ContainedItem is not null) return;

            if (!slot.IsBatterySlot()) throw new ArgumentException("Slot is not a battery slot");

            Item battery = null;
            if (slot.CheckCompatibility(_aaBatteryTemplate))
                battery = _aaBatteryTemplate.CloneItem();
            if (slot.CheckCompatibility(_cr2032BatteryTemplate))
                battery = _cr2032BatteryTemplate.CloneItem();
            if (slot.CheckCompatibility(_cr123ABatteryTemplate))
                battery = _cr123ABatteryTemplate.CloneItem();

            if (battery == null) return;

            slot.Add(battery, false);
            DrainSpawnedBattery(battery, botPlayer);
        }
    }

    private static void DrainSpawnedBattery(Item spawnedBattery, Player botPlayer)
    {
        // Battery charge depends on their max charge and bot level
        if (!spawnedBattery.TryGetItemComponent(out ResourceComponent resourceComponent))
        {
            throw new InvalidOperationException("Spawned battery for bot does not have a resource component");
        }

        // TODO: Make configurable
        var maxValue = (int)resourceComponent.MaxResource;
        int baseValue;
        float levelFactor = Mathf.Clamp01(botPlayer.Profile.Info.Level / 42f /* Highest trader level requirement */);

        if (botPlayer.Side is EPlayerSide.Usec or EPlayerSide.Bear)
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
        if (botPlayer.AIData?.BotOwner != null && botPlayer.AIData.BotOwner.Boss?.IamBoss == true)
        {
            baseValue = maxValue;
        }

        // TODO: Revisit
        var randomCharge = _random.Next(baseValue - 5, baseValue + 10);
        resourceComponent.Value = Mathf.Clamp(randomCharge, 0, maxValue);
        LoggerUtil.Debug($"Set bot's device's battery to {randomCharge}");
    }
}
