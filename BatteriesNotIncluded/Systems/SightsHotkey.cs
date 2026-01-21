using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Systems;

public class SightsHotkey : ISystem
{
    public void Run(DeviceManager manager)
    {
        if (!Input.GetKeyUp(BatteriesNotIncluded.SightsHotkey.Value)) return;

        var mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
        if (mainPlayer == null)
        {
            LoggerUtil.Error("SightHotkey: MainPlayer could not be found");
            return;
        }

        if (mainPlayer.HandsController is not Player.FirearmController firearmController) return;

        var weapon = firearmController.Weapon;
        ToggleSightRecursive(weapon);
    }

    private static void ToggleSightRecursive(CompoundItem item)
    {
        foreach (var slot in item.Slots)
        {
            var containedItem = slot.ContainedItem;
            if (containedItem is SightsItemClass sight)
            {
                sight.ToggleItem();
            }
            if (containedItem is CompoundItem compoundItem)
            {
                ToggleSightRecursive(compoundItem);
            }
        }
    }
}
