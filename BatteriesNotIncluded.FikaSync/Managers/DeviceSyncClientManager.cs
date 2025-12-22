using System.Collections.Generic;
using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Patches;
using BatteriesNotIncluded.FikaSync.Pools;
using BatteriesNotIncluded.FikaSync.Systems;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Systems;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.FikaSync.Managers;

public class DeviceSyncClientManager : BaseSyncManager
{
    public readonly DeviceStateSyncSystem DeviceStateSyncSystem = new();
    public readonly DeviceEnforcementSystem DeviceEnforcementSystem = new();
    public readonly ResourceSyncSystem ResourceSyncSystem = new();
    public readonly BotBatterySyncSystem BotBatterySyncSystem = new();

    private readonly List<Item> _itemsScratch = [];

    public static DeviceSyncClientManager Create(DeviceManager deviceManager)
    {
        DeviceSyncClientManager syncManager = deviceManager.gameObject.AddComponent<DeviceSyncClientManager>();
        syncManager.DeviceManager = deviceManager;

        CorpseInventoryPatch.OnCorpseNewInventory += syncManager.OnCorpseNewInventory;

        return syncManager;
    }

    public void OnDestroy()
    {
        CorpseInventoryPatch.OnCorpseNewInventory -= OnCorpseNewInventory;
        DeviceSubPacketPoolManager.Release();
    }

    public void OnDevicePacketReceived(DevicePacket devicePacket)
    {
        var index = DeviceManager.GetItemIndex(devicePacket.DeviceId);
        if (index == -1) return;

        devicePacket.Execute(this, index);
    }

    private void OnCorpseNewInventory(InventoryEquipment inventoryEquipment)
    {
        inventoryEquipment.GetAllItemsNonAlloc(_itemsScratch, false, false);
        foreach (var item in _itemsScratch)
        {
            RegisterItem(item);
        }

        _itemsScratch.Clear();
    }

    private void RegisterItem(Item item)
    {
        if (item is not CompoundItem compoundItem) return;
        if (!BatteriesNotIncluded.GetDeviceData(compoundItem.TemplateId, out var deviceData)) return;

        var batterySlots = compoundItem.GetBatterySlots(deviceData.SlotCount);
        DeviceManager.Add(compoundItem, batterySlots, ref deviceData);
    }
}
