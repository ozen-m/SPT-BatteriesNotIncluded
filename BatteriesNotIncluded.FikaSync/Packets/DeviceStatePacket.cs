using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace BatteriesNotIncluded.FikaSync.Packets;

public class DeviceStatePacket : IDevicePoolSubPacket
{
    public bool IsOperable;
    public bool IsActive;

    public static DeviceStatePacket CreateInstance()
    {
        return new DeviceStatePacket();
    }

    public static DeviceStatePacket FromValue(bool isOperable, bool isActive)
    {
        var packet = DeviceSubPacketPoolManager.Instance.GetPacket<DeviceStatePacket>(EDeviceSubPacketType.DeviceState);
        packet.IsOperable = isOperable;
        packet.IsActive = isActive;
        return packet;
    }

    public void Execute(DeviceSyncClientManager manager, int deviceIndex)
    {
        manager.DeviceStateSyncSystem.Set(this);
        manager.DeviceStateSyncSystem.Run(manager.DeviceManager, deviceIndex);
        manager.DeviceStateSyncSystem.Set(null);
        manager.DeviceEnforcementSystem.Run(manager.DeviceManager, deviceIndex);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(IsOperable);
        writer.Put(IsActive);
    }

    public void Deserialize(NetDataReader reader)
    {
        IsOperable = reader.GetBool();
        IsActive = reader.GetBool();
    }

    public void Dispose()
    {
        IsOperable = false;
        IsActive = false;
    }

    public void Execute(FikaPlayer player = null)
    {
        // Do nothing
    }
}
