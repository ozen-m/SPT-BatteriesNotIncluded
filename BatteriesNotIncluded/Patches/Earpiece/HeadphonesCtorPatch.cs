using System.Linq;
using System.Reflection;
using BatteriesNotIncluded.Utils;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Earpiece;

/// <summary>
/// Add togglable component to HeadphonesItemClass
/// </summary>
public class HeadphonesCtorPatchPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HeadphonesItemClass).GetConstructors().Single();
    }

    [PatchPostfix]
    protected static void Postfix(HeadphonesItemClass __instance)
    {
        if (!__instance.IsBatteryOperated()) return;

        __instance.Togglable = new TogglableComponent(__instance);
        __instance.Components.Add(__instance.Togglable);
    }
}
