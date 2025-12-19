using BatteriesNotIncluded.FikaSync.Packets;
using Fika.Core.Networking.Pooling;

namespace BatteriesNotIncluded.FikaSync.Pools;

/// <summary>
/// Describes sub-packet types related to battery operated device actions.
/// </summary>
public class DeviceSubPacketPoolManager : BasePacketPoolManager<EDeviceSubPacketType, IDevicePoolSubPacket>
{
    public static DeviceSubPacketPoolManager Instance { get; } = new();

    public static void Release()
    {
        Instance.ClearPool();
    }

    private DeviceSubPacketPoolManager()
    {
        _subPacketFactories =
        [
            DeviceOperablePacket.CreateInstance, // EDeviceSubPacketType.DeviceOperable = 0
            DeviceActivePacket.CreateInstance, // EDeviceSubPacketType.DeviceActive = 1
            ResourceDrainPacket.CreateInstance, // EDeviceSubPacketType.ResourceDrain = 2
        ];
    }
}
