using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Utils;
using BatteriesNotIncluded.Managers;
using EFT.InventoryLogic;

namespace BatteriesNotIncluded.FikaSync.Systems;

public class ResourceSyncSystem : PacketBaseSystem<ResourceDrainPacket>
{
    public override void Run(DeviceManager manager, int i)
    {
        var slot = manager.BatterySlots[i][CurrentPacket.SlotIndex];
        if (slot.ContainedItem is not { } item)
        {
            LoggerUtil.Warning($"Tried to drain battery from {slot} while slot.ContainedItem is null");
            return;
        }
        if (!item.TryGetItemComponent(out ResourceComponent resourceComponent))
        {
            LoggerUtil.Warning($"Tried to drain battery from {slot} while ResourceComponent is not found");
            return;
        }

        var currentCharge = CurrentPacket.Value;
        resourceComponent.Value = currentCharge;

        // Probably not needed, but nice to have
        // item.RaiseRefreshEvent(false, false);

        // Warning: spams
        LoggerUtil.Debug($"Drained item {manager.Devices[i].LocalizedShortName()} {manager.Devices[i].Id} to {currentCharge}");
    }
}
