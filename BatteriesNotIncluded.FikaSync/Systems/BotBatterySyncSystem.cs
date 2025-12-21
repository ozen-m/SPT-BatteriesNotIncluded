using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Utils;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.FikaSync.Systems;

public class BotBatterySyncSystem : PacketBaseSystem<BotBatteryPacket>
{
    public override void Run(DeviceManager manager, int i)
    {
        var slot = manager.BatterySlots[i][CurrentPacket.SlotIndex];

        var addOp = slot.Add(CurrentPacket.Battery, false);
        if (addOp.Failed)
        {
            LoggerUtil.Warning($"Received packet to add bot's device battery but failed: {addOp.Error} ({CurrentPacket.Battery} to {slot})");
        }
    }
}
