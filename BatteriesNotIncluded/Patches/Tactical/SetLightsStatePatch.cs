using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Tactical;

public class SetLightsStatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.SetLightsState));
    }

    [PatchPrefix]
    protected static void Prefix(FirearmLightStateStruct[] lightsStates)
    {
        if (!Singleton<DeviceManager>.Instantiated) return;

        var manager = Singleton<DeviceManager>.Instance;
        for (var i = 0; i < lightsStates.Length; i++)
        {
            ref var lightsState = ref lightsStates[i];

            // If player wants light on but has no/drained batteries, don't allow
            if (lightsState.IsActive && !manager.GetIsOperable(lightsState.Id))
            {
                lightsState.IsActive = false;

                // bool __result is used to check if to Player.PlayTacticalSound(), we want to play sound.
                // __result = false; 
            }
        }
    }
}
