using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using Fika.Core.Networking.LiteNetLib.Utils;
using Fika.Core.Networking.Packets;

namespace BatteriesNotIncluded.FikaSync.Packets;

public class DevicePacket : INetReusable
{
    // TODO: Switch to DeviceId
    public int DeviceIndex;
    public IDevicePoolSubPacket SubPacket;
    public EDeviceSubPacketType Type;

    public void Execute(DeviceSyncClientManager manager, int deviceIndex)
    {
        SubPacket.Execute(manager, deviceIndex);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(DeviceIndex);
        writer.PutEnum(Type);
        SubPacket?.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        DeviceIndex = reader.GetInt();
        Type = reader.GetEnum<EDeviceSubPacketType>();
        SubPacket = DeviceSubPacketPoolManager.Instance.GetPacket<IDevicePoolSubPacket>(Type);
        SubPacket.Deserialize(reader);
    }

    public void Clear()
    {
        if (SubPacket == null) return;

        DeviceSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
        DeviceIndex = -1;
        Type = default;
        SubPacket = null;
    }

    public void Flush()
    {
        DeviceSubPacketPoolManager.Instance.ReturnPacket(Type, SubPacket);
        DeviceIndex = -1;
        Type = default;
        SubPacket = null;
    }
}
