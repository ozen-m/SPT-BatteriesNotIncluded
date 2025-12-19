using BatteriesNotIncluded.FikaSync.Packets;
using BatteriesNotIncluded.FikaSync.Pools;
using BatteriesNotIncluded.FikaSync.Systems;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.FikaSync.Managers;

public class DeviceSyncClientManager : BaseSyncManager
{
    public OperableSyncSystem OperableSyncSystem;
    public ActiveSyncSystem ActiveSyncSystem;
    public ResourceSyncSystem ResourceSyncSystem;

    public static DeviceSyncClientManager Create(DeviceManager deviceManager)
    {
        DeviceSyncClientManager syncManager = deviceManager.gameObject.AddComponent<DeviceSyncClientManager>();
        syncManager.DeviceManager = deviceManager;
        syncManager.OperableSyncSystem = new OperableSyncSystem();
        syncManager.ActiveSyncSystem = new ActiveSyncSystem();
        syncManager.ResourceSyncSystem = new ResourceSyncSystem();

        return syncManager;
    }

    public void OnDestroy()
    {
        DeviceSubPacketPoolManager.Release();
    }

    public void OnDevicePacketReceived(DevicePacket devicePacket)
    {
        devicePacket.Execute(this, devicePacket.DeviceIndex);
    }
}
