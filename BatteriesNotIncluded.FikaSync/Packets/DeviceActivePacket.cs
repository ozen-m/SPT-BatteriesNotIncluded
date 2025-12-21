using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace BatteriesNotIncluded.FikaSync.Packets;

public class DeviceActivePacket : IDevicePoolSubPacket
{
    public bool IsActive;

    public static DeviceActivePacket CreateInstance()
    {
        return new DeviceActivePacket();
    }

    public static DeviceActivePacket FromValue(bool isActive)
    {
        var packet = DeviceSubPacketPoolManager.Instance.GetPacket<DeviceActivePacket>(EDeviceSubPacketType.DeviceActive);
        packet.IsActive = isActive;
        return packet;
    }

    public void Execute(DeviceSyncClientManager manager, int deviceIndex)
    {
        manager.ActiveSyncSystem.Set(this);
        manager.ActiveSyncSystem.Run(manager.DeviceManager, deviceIndex);
        manager.ActiveSyncSystem.Set(null);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(IsActive);
    }

    public void Deserialize(NetDataReader reader)
    {
        IsActive = reader.GetBool();
    }

    public void Dispose()
    {
        IsActive = false;
    }

    public void Execute(FikaPlayer player = null)
    {
        throw new System.NotImplementedException();
    }
}
