using System.Linq;
using System.Reflection;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Sight;

public class SightsItemCtorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(SightsItemClass).GetConstructors().Single();
    }

    [PatchPostfix]
    protected static void Postfix(SightsItemClass __instance)
    {
        if (!__instance.IsBatteryOperated()) return;
        if (__instance is NightVisionItemClass or ThermalVisionItemClass) return;

        var togglable = new TogglableComponent(__instance);
        __instance.Components.Add(togglable);

        // Default to on state
        togglable.Set(true, false, true);
    }
}
