namespace BatteriesNotIncluded.FikaSync.Packets;

public enum EDeviceSubPacketType : byte
{
    /// <summary>
    /// Set device operable status
    /// </summary>
    DeviceOperable,

    /// <summary>
    /// Set device active status
    /// </summary>
    DeviceActive,

    /// <summary>
    /// Drained battery of device
    /// </summary>
    ResourceDrain
}
