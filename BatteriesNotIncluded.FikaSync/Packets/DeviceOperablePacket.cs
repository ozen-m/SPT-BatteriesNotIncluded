using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace BatteriesNotIncluded.FikaSync.Packets;

public class DeviceOperablePacket : IDevicePoolSubPacket
{
    public bool IsPrevOperable;
    public bool IsOperable;

    public static DeviceOperablePacket CreateInstance()
    {
        return new DeviceOperablePacket();
    }

    public static DeviceOperablePacket FromValue(bool isPrevOperable, bool isOperable)
    {
        var packet = DeviceSubPacketPoolManager.Instance.GetPacket<DeviceOperablePacket>(EDeviceSubPacketType.DeviceOperable);
        packet.IsPrevOperable = isPrevOperable;
        packet.IsOperable = isOperable;
        return packet;
    }

    public void Execute(DeviceSyncClientManager manager, int deviceIndex)
    {
        manager.OperableSyncSystem.Set(this);
        manager.OperableSyncSystem.Run(manager.DeviceManager, deviceIndex);
        manager.OperableSyncSystem.Set(null);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(IsPrevOperable);
        writer.Put(IsOperable);
    }

    public void Deserialize(NetDataReader reader)
    {
        IsPrevOperable = reader.GetBool();
        IsOperable = reader.GetBool();
    }

    public void Dispose()
    {
        IsPrevOperable = false;
        IsOperable = false;
    }

    public void Execute(FikaPlayer player = null)
    {
        // Do nothing
    }
}
