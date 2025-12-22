using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace BatteriesNotIncluded.FikaSync.Packets;

public class ResourceDrainPacket : IDevicePoolSubPacket
{
    public int SlotIndex;
    public float Value;

    public static ResourceDrainPacket CreateInstance()
    {
        return new ResourceDrainPacket();
    }

    public static ResourceDrainPacket FromValue(int slotIndex, float value)
    {
        var packet = DeviceSubPacketPoolManager.Instance.GetPacket<ResourceDrainPacket>(EDeviceSubPacketType.ResourceDrain);
        packet.SlotIndex = slotIndex;
        packet.Value = value;
        return packet;
    }

    public void Execute(DeviceSyncClientManager manager, int deviceIndex)
    {
        manager.ResourceSyncSystem.Set(this);
        manager.ResourceSyncSystem.Run(manager.DeviceManager, deviceIndex);
        manager.ResourceSyncSystem.Set(null);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SlotIndex);
        writer.Put(Value);
    }

    public void Deserialize(NetDataReader reader)
    {
        SlotIndex = reader.GetInt();
        Value = reader.GetFloat();
    }

    public void Dispose()
    {
        SlotIndex = -1;
        Value = -1;
    }

    public void Execute(FikaPlayer player = null)
    {
        // Do nothing
    }
}
