using System.Reflection;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using EFT.Communications;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BatteriesNotIncluded.Patches.LifeCycle;

public class GameWorldCreatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.Create))!.MakeGenericMethod(typeof(ClientLocalGameWorld));
    }

    [PatchPostfix]
    public static void Postfix(GameWorld __result)
    {
        Fika.IsFikaSyncPresent = Chainloader.PluginInfos.ContainsKey("com.ozen.batteriesnotincluded.fikasync");
        if (Fika.IsFikaPresent && !Fika.IsFikaSyncPresent)
        {
            BatteriesNotIncluded.DisablePatches();
            LoggerUtil.Error("Disabled mod Batteries Not Included. Fika is present but the sync addon is missing, please install the sync addon.");
            NotificationManagerClass.DisplayWarningNotification(
                "Disabled mod Batteries Not Included. Fika is present but the sync addon is missing, please install the sync addon.",
                ENotificationDurationType.Infinite
            );

            return;
        }

        var manager = Singleton<DeviceManager>.Instance;
        if (manager != null)
        {
            // Probably only caused by the hideout
            LoggerUtil.Info("DeviceManager already instantiated, destroying.");
            Object.DestroyImmediate(manager);
        }

        manager = __result.gameObject.AddComponent<DeviceManager>();
        manager.SubscribeToGameWorld(__result);
        Singleton<DeviceManager>.Create(manager);

        LoggerUtil.Info("DeviceManager created");
    }
}
