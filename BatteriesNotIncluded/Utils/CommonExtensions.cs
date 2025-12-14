using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Utils;

public static class CommonExtensions
{
    public const string AABatteryId = "5672cb124bdc2d1a0f8b4568";
    public const string CR2032BatteryId = "5672cb304bdc2dc2088b456a";
    public const string CR123ABatteryId = "590a358486f77429692b2790";

    private static readonly HashSet<MongoID> _batteryIds = [AABatteryId, CR2032BatteryId, CR123ABatteryId];

    public static bool IsBatteryOperated(this Item item) => item is CompoundItem compoundItem && compoundItem.IsBatteryOperated();

    public static bool IsBatteryOperated(this CompoundItem item)
    {
        return IsBatteryOperated(item, out _);
    }

    public static bool IsBatteryOperated(this CompoundItem item, out Slot[] batterySlot)
    {
        // Get slot number from DeviceData, new Slot[DeviceData.Slots];
        List<Slot> slots = [];
        foreach (var slot in item.Slots)
        {
            if (slot.IsBatterySlot())
            {
                slots.Add(slot);
            }
        }
        batterySlot = slots.ToArray();
        return slots.Count > 0;
    }

    public static bool IsBattery(this Item item) => _batteryIds.Contains(item.TemplateId);

    public static int GetBatterySlotIndex(this Item item)
    {
        if (item is not CompoundItem compoundItem) return -1;

        return compoundItem.GetBatterySlotIndex();
    }

    // TODO: Multiple battery slots? Or drain faster?
    public static int GetBatterySlotIndex(this CompoundItem item)
    {
        var slots = item.Slots;
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsBatterySlot())
            {
                return i;
            }
        }
        return -1;
    }

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
}
