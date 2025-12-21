using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Systems;

namespace BatteriesNotIncluded.FikaSync.Systems;

public abstract class PacketBaseSystem<TPacket> : BaseSystem where TPacket : IDevicePoolSubPacket
{
    protected TPacket CurrentPacket;

    public void Set(TPacket packet)
    {
        CurrentPacket = packet;
    }

    public override void Run(DeviceManager manager)
    {
    }
}
