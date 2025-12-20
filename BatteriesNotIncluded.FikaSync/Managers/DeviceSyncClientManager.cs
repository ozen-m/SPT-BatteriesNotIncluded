using System.Collections.Generic;
using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Patches;
using BatteriesNotIncluded.FikaSync.Pools;
using BatteriesNotIncluded.FikaSync.Systems;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.FikaSync.Managers;

public class DeviceSyncClientManager : BaseSyncManager
{
    public readonly OperableSyncSystem OperableSyncSystem = new();
    public readonly ActiveSyncSystem ActiveSyncSystem = new();
    public readonly ResourceSyncSystem ResourceSyncSystem = new();

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
        devicePacket.Execute(this, index);
    }

    public void OnBotBatteryPacketReceived(BotBatteryPacket packet)
    {
        var deviceIndex = DeviceManager.GetItemIndex(packet.DeviceId);
        var slot = DeviceManager.BatterySlots[deviceIndex][packet.SlotIndex];
        packet.Battery.CurrentAddress = null; // Is this needed?
        var addOp = slot.Add(packet.Battery, false);
        if (addOp.Failed)
        {
            LoggerUtil.Warning($"Received packet to add bot's device battery but failed: {addOp.Error} ({packet.Battery} to {slot})");
        }
    }

    public void OnCorpseNewInventory(InventoryEquipment inventoryEquipment)
    {
        inventoryEquipment.GetAllItemsNonAlloc(_itemsScratch, false, false);
        foreach (var item in _itemsScratch)
        {
            RegisterItem(item);
        }

        _itemsScratch.Clear();
    }

    public void RegisterItem(Item item)
    {
        if (item is not CompoundItem compoundItem) return;
        if (!BatteriesNotIncluded.GetDeviceData(compoundItem.TemplateId, out var deviceData)) return;

        var batterySlots = compoundItem.GetBatterySlots(deviceData.SlotCount);
        DeviceManager.Add(compoundItem, batterySlots, ref deviceData);
    }
}
