using System.Collections.Generic;
using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Patches;
using BatteriesNotIncluded.FikaSync.Pools;
using BatteriesNotIncluded.FikaSync.Systems;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Systems;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;
using Fika.Core.Networking;

namespace BatteriesNotIncluded.FikaSync.Managers;

public class DeviceSyncClientManager : BaseSyncManager
{
    public readonly DeviceStateSyncSystem DeviceStateSyncSystem = new();
    public readonly DeviceEnforcementSystem DeviceEnforcementSystem = new();
    public readonly ResourceSyncSystem ResourceSyncSystem = new();
    public readonly BotBatterySyncSystem BotBatterySyncSystem = new();

    private readonly List<Item> _itemsScratch = [];

    public static DeviceSyncClientManager Create(DeviceManager deviceManager, FikaClient fikaClient)
    {
        DeviceSyncClientManager syncManager = deviceManager.gameObject.AddComponent<DeviceSyncClientManager>();
        syncManager.DeviceManager = deviceManager;

        fikaClient.RegisterNetReusable<DevicePacket>(syncManager.OnDevicePacketReceived);
        CorpseInventoryPatch.OnCorpseNewInventory += syncManager.OnCorpseNewInventory;

        return syncManager;
    }

    public void OnDestroy()
    {
        CorpseInventoryPatch.OnCorpseNewInventory -= OnCorpseNewInventory;
        DeviceSubPacketPoolManager.Release();
    }

    private void OnDevicePacketReceived(DevicePacket devicePacket)
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
        if (!BatteriesNotIncluded.GetDeviceData(item.TemplateId, out var deviceData)) return;
        if (item is not CompoundItem compoundItem) return;

        var batterySlots = compoundItem.GetBatterySlots(deviceData.SlotCount);
        DeviceManager.Add(compoundItem, batterySlots, ref deviceData);
    }
}
