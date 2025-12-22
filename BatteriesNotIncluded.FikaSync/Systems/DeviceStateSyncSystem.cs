using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.FikaSync.Systems;

public class DeviceStateSyncSystem : PacketBaseSystem<DeviceStatePacket>
{
    public override void Run(DeviceManager manager, int i)
    {
        manager.IsPrevOperable[i] = manager.IsOperable[i];
        manager.IsOperable[i] = CurrentPacket.IsOperable;

        manager.IsPrevActive[i] = manager.IsActive[i];
        manager.IsActive[i] = CurrentPacket.IsActive;
    }
}
