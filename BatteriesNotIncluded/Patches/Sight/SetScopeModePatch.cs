using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Sight;

/// <summary>
/// Run manual systems on change scope mode
/// </summary>
public class SetScopeModePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.SetScopeMode));
    }

    [PatchPostfix]
    protected static void Postfix(Player.FirearmController __instance, FirearmScopeStateStruct[] scopeStates)
    {
        var manager = Singleton<DeviceManager>.Instance;
        for (var i = 0; i < scopeStates.Length; i++)
        {
            ref var scopeState = ref scopeStates[i];
            if (!manager.IsItemRegistered(scopeState.Id)) continue;

            // Set optic visibility here
            manager.RunManualUpdateNextFrame();
            return;
        }
    }
}
