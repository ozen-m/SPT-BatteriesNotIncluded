using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Utils;
using BatteriesNotIncluded.Managers;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.FikaSync.Systems;

public class BotBatterySyncSystem : PacketBaseSystem<BotBatteryPacket>
{
    public override void Run(DeviceManager manager, int i)
    {
        var slot = manager.BatterySlots[i][CurrentPacket.SlotIndex];

        var addOp = slot.Add(CurrentPacket.Battery, false);
        switch (addOp)
        {
            // Inventory Errors/Slot not empty
            case { Failed: true, Error: not Slot.GClass1578 }:
                LoggerUtil.Warning($"Failed to add {CurrentPacket.Battery} to {slot}: {addOp.Error}");
                return;
            // Handle case where battery is already existing
            case { Failed: true, Error: Slot.GClass1578 }:
            {
                if (slot.ContainedItem!.TryGetItemComponent<ResourceComponent>(out var resourceComponent))
                {
                    resourceComponent.Value = CurrentPacket.Battery.GetItemComponent<ResourceComponent>()!.Value;
                }
                return;
            }
            default:
                LoggerUtil.Debug($"Set battery {CurrentPacket.Battery} charge to {CurrentPacket.Battery.GetItemComponent<ResourceComponent>()!.Value}");
                return;
        }
    }
}
