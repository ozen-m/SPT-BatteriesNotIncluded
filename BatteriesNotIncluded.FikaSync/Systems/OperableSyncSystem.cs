using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.FikaSync.Systems;

public class OperableSyncSystem : PacketBaseSystem<DeviceOperablePacket>
{
    public override void Run(DeviceManager manager, int i)
    {
        manager.IsPrevOperable[i] = CurrentPacket.IsPrevOperable;
        manager.IsOperable[i] = CurrentPacket.IsOperable;
    }
}
