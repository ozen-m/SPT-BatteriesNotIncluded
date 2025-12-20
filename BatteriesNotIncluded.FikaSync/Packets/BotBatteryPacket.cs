using EFT.InventoryLogic;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace BatteriesNotIncluded.FikaSync.Packets;

public struct BotBatteryPacket : INetSerializable
{
    public Item Battery;
    public int DeviceIndex;
    public int SlotIndex;

    public void Deserialize(NetDataReader reader)
    {
        Battery = reader.GetItem();
        DeviceIndex = reader.GetInt();
        SlotIndex = reader.GetInt();
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutItem(Battery);
        writer.Put(DeviceIndex);
        writer.Put(SlotIndex);
    }
}
