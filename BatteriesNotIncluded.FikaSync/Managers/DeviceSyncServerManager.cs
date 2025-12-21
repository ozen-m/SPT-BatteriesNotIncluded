using System;
using System.Collections.Generic;
using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Pools;
using BatteriesNotIncluded.Managers;
using EFT.InventoryLogic;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;

namespace BatteriesNotIncluded.FikaSync.Managers;

public class DeviceSyncServerManager : BaseSyncManager
{
    private readonly List<Action> _unsubscribeActions = [];

    private DevicePacket _devicePacket = new();
    private FikaServer _fikaServer;

    public static DeviceSyncServerManager Create(DeviceManager deviceManager, FikaServer fikaServer)
    {
        DeviceSyncServerManager syncManager = deviceManager.gameObject.AddComponent<DeviceSyncServerManager>();
        syncManager.DeviceManager = deviceManager;
        syncManager.SubscribeToDeviceManager();
        syncManager._fikaServer = fikaServer;

        return syncManager;
    }

    public void OnDestroy()
    {
        foreach (Action unsubscribe in _unsubscribeActions)
        {
            unsubscribe();
        }
        DeviceSubPacketPoolManager.Release();
    }

    private void SubscribeToDeviceManager()
    {
        if (DeviceManager == null) throw new InvalidOperationException("DeviceManager is not set");

        _unsubscribeActions.Add(DeviceManager.SubscribeToOnSetDeviceOperable(SendDeviceOperablePacket));
        _unsubscribeActions.Add(DeviceManager.SubscribeToOnSetDeviceActive(SendDeviceActivePacket));
        _unsubscribeActions.Add(DeviceManager.SubscribeToOnDrainResource(SendResourceDrainPacket));

        DeviceManager.OnAddBatteryToSlot += OnAddBatteryToSlot;
        _unsubscribeActions.Add(() => DeviceManager.OnAddBatteryToSlot -= OnAddBatteryToSlot);
    }

    private void SendDeviceOperablePacket(string deviceId, bool isPrevOperable, bool isOperable)
    {
        _devicePacket.DeviceId = deviceId;
        _devicePacket.SubPacket = DeviceOperablePacket.FromValue(isPrevOperable, isOperable);
        _devicePacket.Type = EDeviceSubPacketType.DeviceOperable;
        _fikaServer.SendNetReusable(ref _devicePacket, DeliveryMethod.ReliableOrdered);
    }

    private void SendDeviceActivePacket(string deviceId, bool isActive)
    {
        _devicePacket.DeviceId = deviceId;
        _devicePacket.SubPacket = DeviceActivePacket.FromValue(isActive);
        _devicePacket.Type = EDeviceSubPacketType.DeviceActive;
        _fikaServer.SendNetReusable(ref _devicePacket, DeliveryMethod.ReliableOrdered);
    }

    private void SendResourceDrainPacket(string deviceId, int slotIndex, float currentCharge)
    {
        _devicePacket.DeviceId = deviceId;
        _devicePacket.SubPacket = ResourceDrainPacket.FromValue(slotIndex, currentCharge);
        _devicePacket.Type = EDeviceSubPacketType.ResourceDrain;
        _fikaServer.SendNetReusable(ref _devicePacket, DeliveryMethod.ReliableUnordered);
    }

    private void OnAddBatteryToSlot(string deviceId, int slotIndex, Item battery)
    {
        BotBatteryPacket packet = new()
        {
            Battery = battery,
            DeviceId = deviceId,
            SlotIndex = slotIndex
        };

        _fikaServer.SendData(ref packet, DeliveryMethod.ReliableOrdered);
    }
}
