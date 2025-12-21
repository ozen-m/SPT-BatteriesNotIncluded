using System;
using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace BatteriesNotIncluded.FikaSync.Packets;

public class BotBatteryPacket : IDevicePoolSubPacket
{
    public Item Battery;
    public int SlotIndex;

    public static BotBatteryPacket CreateInstance()
    {
        return new BotBatteryPacket();
    }

    public static BotBatteryPacket FromValue(Item battery, int slotIndex)
    {
        var packet = DeviceSubPacketPoolManager.Instance.GetPacket<BotBatteryPacket>(EDeviceSubPacketType.BotBattery);
        packet.Battery = battery;
        packet.SlotIndex = slotIndex;
        return packet;
    }

    public void Execute(DeviceSyncClientManager manager, int deviceIndex)
    {
        manager.BotBatterySyncSystem.Set(this);
        manager.BotBatterySyncSystem.Run(manager.DeviceManager, deviceIndex);
        manager.BotBatterySyncSystem.Set(null);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutItem(Battery);
        writer.Put(SlotIndex);
    }

    public void Deserialize(NetDataReader reader)
    {
        Battery = reader.GetItem();
        SlotIndex = reader.GetInt();
    }

    public void Dispose()
    {
        Battery = null;
        SlotIndex = -1;
    }

    public void Execute(FikaPlayer player = null)
    {
        throw new NotImplementedException();
    }
}
