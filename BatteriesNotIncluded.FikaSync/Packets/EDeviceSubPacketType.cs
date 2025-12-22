namespace BatteriesNotIncluded.FikaSync.Packets;

public enum EDeviceSubPacketType : byte
{
    /// <summary>
    /// Set device state
    /// </summary>
    DeviceState,

    /// <summary>
    /// Drained battery of device
    /// </summary>
    ResourceDrain,

    /// <summary>
    /// Added battery for bot's device
    /// </summary>
    BotBattery
}
