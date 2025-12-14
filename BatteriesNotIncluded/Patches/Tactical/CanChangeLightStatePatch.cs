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
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.SetLightsState));
    }

    [PatchPostfix]
    protected static void Postfix(Player.FirearmController.GClass2013 __instance, ref FirearmLightStateStruct[] lightsStates, ref bool __result)
    {
        var manager = Singleton<DeviceManager>.Instance;
        for (var i = 0; i < lightsStates.Length; i++)
        {
            ref var lightsState = ref lightsStates[i];

            // Light component doesn't have an event we can subscribe to, do it here.
            // IsRegistered check?
            manager.ManualUpdate(lightsState.Id);

            if (lightsState.IsActive && !manager.GetIsOperable(lightsState.Id))
            {
                lightsState.IsActive = false;
            }
        }
    }
}
