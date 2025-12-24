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

        if (BatteriesNotIncluded.HeadphonesTogglableField is not null)
        {
            // Prepatched
            var togglable = new TogglableComponent(__instance);
            BatteriesNotIncluded.HeadphonesTogglableField(__instance) = togglable;
            __instance.Components.Add(togglable);
        }
        else
        {
            var togglable = new TogglableComponent(__instance);
            __instance.Components.Add(togglable);

            // Default to on state
            togglable.Set(true, false, true);
        }
    }
}
