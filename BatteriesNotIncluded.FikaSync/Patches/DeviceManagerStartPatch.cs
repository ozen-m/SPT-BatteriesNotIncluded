using System.Reflection;
using BatteriesNotIncluded.FikaSync.Managers;
using BatteriesNotIncluded.FikaSync.Pools;
using BatteriesNotIncluded.FikaSync.Utils;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using Fika.Core.Networking;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.FikaSync.Patches;

public class DeviceManagerStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(DeviceManager).GetMethod(nameof(DeviceManager.Start));
    }

    [PatchPostfix]
    public static void Postfix(DeviceManager __instance)
    {
        IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
        DeviceSubPacketPoolManager.Instance.CreatePool();

        switch (networkManager)
        {
            case FikaClient client:
            {
                DeviceSyncClientManager.Create(__instance, client);
                break;
            }
            case FikaServer server:
                DeviceSyncServerManager.Create(__instance, server);
                break;
        }
        LoggerUtil.Info("DeviceSyncManager created");
    }
}
