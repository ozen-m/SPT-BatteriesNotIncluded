using System;
using System.Reflection;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Sight;

/// <summary>
/// Capture SightModVisualControllers instances
/// </summary>
public class CaptureSightControllerPatch : ModulePatch
{
    public static event Action<SightModVisualControllers> OnSetSightMode;

    protected override MethodBase GetTargetMethod()
    {
        // Use set_SightMod instead of Awake/Init, instead of checking if SightMod.Item is battery operated since
        // Awake/Init doesn't yet set SightModVisualControllers.SightMod.
        return typeof(SightModVisualControllers).GetMethod("set_SightMod");
    }

    [PatchPostfix]
    public static void Postfix(SightModVisualControllers __instance)
    {
        OnSetSightMode?.Invoke(__instance);
    }
}
