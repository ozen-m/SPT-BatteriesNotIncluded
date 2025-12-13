using System;
using System.Reflection;
using System.Threading.Tasks;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using UnityEngine;
using Random = System.Random;

namespace BatteriesNotIncluded.Patches;

public class PlayerInitPatch : ModulePatch
{
    private static readonly Random _random = new();
    private static Item _aaBatteryTemplate;
    private static Item _cr2032BatteryTemplate;
    private static Item _cr123ABatteryTemplate;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.Init));
    }

    [PatchPostfix]
    protected static async void Postfix(Player __instance, Task __result)
    {
        if (!Singleton<DeviceManager>.Instantiated)
        {
            LoggerUtil.Error("DeviceManager not instantiated");
            return;
        }
        var manager = Singleton<DeviceManager>.Instance;

        await __result;

        var allPlayerItems = __instance.Inventory.Equipment.GetAllItems();
        foreach (var item in allPlayerItems)
        {
            if (item is not CompoundItem compoundItem || !compoundItem.IsBatteryOperated(out var batterySlot)) continue;

            var batteryData = BatteriesNotIncluded.GetBatteryData(compoundItem.TemplateId);
            manager.Add(compoundItem, ref batteryData, batterySlot);

            if (!__instance.IsYourPlayer)
            {
                AddBatteriesToBotDevice(batterySlot, __instance);
            }
        }

        // TODO: Add devices of loot in world
    }

    private static void AddBatteriesToBotDevice(Slot slot, Player botPlayer)
    {
        _aaBatteryTemplate ??= Singleton<ItemFactoryClass>.Instance.GetPresetItem(CommonExtensions.AABatteryId);
        _cr2032BatteryTemplate ??= Singleton<ItemFactoryClass>.Instance.GetPresetItem(CommonExtensions.CR2032BatteryId);
        _cr123ABatteryTemplate ??= Singleton<ItemFactoryClass>.Instance.GetPresetItem(CommonExtensions.CR123ABatteryId);

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
