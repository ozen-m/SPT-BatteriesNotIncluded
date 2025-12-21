using BatteriesNotIncluded.FikaSync.Managers;
using Fika.Core.Networking.Packets;

namespace BatteriesNotIncluded.FikaSync.Packets;

public interface IDevicePoolSubPacket : IPoolSubPacket
{
    public void Execute(DeviceSyncClientManager manager, int deviceIndex);
}
