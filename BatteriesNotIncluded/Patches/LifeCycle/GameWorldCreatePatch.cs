using System.Reflection;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.LifeCycle;

public class GameWorldCreatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.Create))!.MakeGenericMethod(typeof(ClientLocalGameWorld));
    }

    [PatchPostfix]
    protected static void Postfix(GameWorld __result)
    {
        if (Singleton<DeviceManager>.Instantiated)
        {
            LoggerUtil.Error("DeviceManager already instantiated");
            return;
        }

        Fika.IsFikaSyncPresent = Chainloader.PluginInfos.ContainsKey("com.ozen.batteriesnotincluded.fikasync");
        if (Fika.IsFikaPresent && !Fika.IsFikaSyncPresent)
        {
            BatteriesNotIncluded.DisablePatches();
            LoggerUtil.Error("Fika is present but the sync addon is missing, please install the sync addon. Disabled mod Batteries Not Included.");
            return;
        }

        var manager = __result.gameObject.AddComponent<DeviceManager>();
        manager.SubscribeToGameWorld(__result);
        Singleton<DeviceManager>.Create(manager);

        LoggerUtil.Info("DeviceManager created");
    }
}
