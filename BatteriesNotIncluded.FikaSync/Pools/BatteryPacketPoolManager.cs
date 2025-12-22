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
            DeviceStatePacket.CreateInstance, // EDeviceSubPacketType.DeviceState = 0
            ResourceDrainPacket.CreateInstance, // EDeviceSubPacketType.ResourceDrain = 1
            BotBatteryPacket.CreateInstance // EDeviceSubPacketType.BotBattery = 2
        ];
    }
}
