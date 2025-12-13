using System;
using System.Reflection;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Sight;

/// <summary>
/// Capture SightModVisualControllers instances
/// </summary>
public class CaptureSightControllerPatch : ModulePatch
{
    public static event Action<SightModVisualControllers> OnUpdateSightMode;

    protected override MethodBase GetTargetMethod()
    {
        // Use set_SightMod instead of Awake/Init, instead of checking if sightmod.item is battery operated since
        // Awake/Init doesn't yet set SightModVisualControllers.SightMod.
        return typeof(SightModVisualControllers).GetMethod("set_SightMod");
    }

    [PatchPostfix]
    protected static void Postfix(SightModVisualControllers __instance)
    {
        OnUpdateSightMode?.Invoke(__instance);
    }
}
