using System.Collections.Generic;
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
    protected static void Postfix(ProceduralWeaponAnimation __instance, List<ProceduralWeaponAnimation.SightNBone> ____optics)
    {
        var manager = Singleton<DeviceManager>.Instance;
        foreach (var sight in __instance.ScopeAimTransforms)
        {
            if (sight.ScopePrefabCache == null) continue;

            var item = sight.Mod.Item;
#if DEBUG
            if (manager.IsItemRegistered(item.Id))
#endif
                manager.UpdateSightVisibility(item);
        }
    }
}
