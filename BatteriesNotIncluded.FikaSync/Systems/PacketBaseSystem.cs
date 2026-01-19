using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Systems;

namespace BatteriesNotIncluded.FikaSync.Systems;

public abstract class PacketBaseSystem<TPacket> : IManualSystem where TPacket : IDevicePoolSubPacket
{
    protected TPacket CurrentPacket;

    public abstract void Run(DeviceManager manager, int i);
    
    public void Set(TPacket packet)
    {
        CurrentPacket = packet;
    }
}
