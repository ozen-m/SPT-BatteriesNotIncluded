using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.LifeCycle;

public class OnGameStartedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    protected static void Postfix()
    {
        if (!Singleton<DeviceManager>.Instantiated) return;

        Singleton<DeviceManager>.Instance.RunAfterGameStart();
    }
}
