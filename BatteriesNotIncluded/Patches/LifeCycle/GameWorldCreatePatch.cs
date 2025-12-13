using System.Reflection;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
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

        var manager = __result.GetOrAddComponent<DeviceManager>();
        Singleton<DeviceManager>.Create(manager);
        LoggerUtil.Debug("DeviceManager created");
    }
}
