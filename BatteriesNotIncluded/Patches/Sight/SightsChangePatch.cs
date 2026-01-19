using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT.Animations;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Sight;

/// <summary>
/// UpdateSightVisibility on actions (equip gun, ADS, change sight mode)
/// </summary>
public class SightsChangePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ProceduralWeaponAnimation).GetMethod(nameof(ProceduralWeaponAnimation.method_1));
    }

    [PatchPostfix]
    public static void Postfix(ProceduralWeaponAnimation __instance)
    {
        if (!__instance.FirstPersonPointOfView) return;

        var manager = Singleton<DeviceManager>.Instance;
        if (manager == null) return;

        foreach (var sight in __instance.ScopeAimTransforms)
        {
            if (sight.ScopePrefabCache == null) continue;

            manager.EnforceSightVisibility(sight.Mod.Item);
        }
    }
}
