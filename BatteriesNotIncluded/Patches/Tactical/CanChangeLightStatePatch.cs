using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Tactical;

public class CanChangeLightStatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController.GClass2013)
            .GetMethod(nameof(Player.FirearmController.GClass2013.CanChangeLightState));
    }

    // BUG: Hold tactical devices does not stop draining battery

    [PatchPostfix]
    protected static void Postfix(Player.FirearmController.GClass2013 __instance, ref FirearmLightStateStruct[] lightsStates, ref bool __result)
    {
        if (!__result) return;

        var manager = Singleton<DeviceManager>.Instance;
        for (var i = 0; i < lightsStates.Length; i++)
        {
            ref var lightsState = ref lightsStates[i];
            if (manager.IsItemRegistered(lightsState.Id))
            {
                // Light component doesn't have an event we can subscribe to, do it here.
                manager.RunManualUpdateNextFrame();
            }
            if (lightsState.IsActive && !manager.GetIsOperable(lightsState.Id))
            {
                lightsState.IsActive = false;
            }
        }
    }
}
