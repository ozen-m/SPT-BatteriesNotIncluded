using BatteriesNotIncluded.Managers;
using BepInEx;
using BepInEx.Logging;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using SPT.Reflection.Patching;

#pragma warning disable CA2211

namespace BatteriesNotIncluded.FikaSync;

[BepInPlugin("com.ozen.batteriesnotincluded.fikasync", "Batteries Not Included Fika Sync", "1.0.2")]
[BepInDependency("com.ozen.batteriesnotincluded")]
[BepInDependency("com.fika.core")]
public class BatteriesNotIncludedSync : BaseUnityPlugin
{
    public static ManualLogSource LogSource;

    protected void Awake()
    {
        LogSource = Logger;

        var patchManager = new PatchManager(this, true);
        patchManager.EnablePatches();

        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreatedEvent);
    }

    private static void OnFikaNetworkManagerCreatedEvent(FikaNetworkManagerCreatedEvent createNetworkManager)
    {
        switch (createNetworkManager.Manager)
        {
            case FikaClient:
                ExternalMod.Fika.IsServer = false;
                return;
            case FikaServer:
                ExternalMod.Fika.IsServer = true;
                return;
        }
    }
}
