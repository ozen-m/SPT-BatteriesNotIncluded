using System.Reflection;
using BatteriesNotIncluded.Managers;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Headwear;

/// <summary>
/// Patch to not enable NightVision.SwitchComponentsOn if nvg is inoperable (no/drained battery).
/// Called when NVGs are turned on/off.
/// </summary>
public class NightVisionOnPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(NightVision).GetMethod(nameof(NightVision.method_1));
    }

    [PatchPrefix]
    protected static bool Prefix(NightVision __instance, bool on)
    {
        if (!on) return true;

        NightVisionComponent component = GamePlayerOwner.MyPlayer.NightVisionObserver.Component;
        if (component is not null && !Singleton<DeviceManager>.Instance.GetIsOperable(component.Item))
        {
            // Skip turning on components, but enable mask
            __instance.TextureMask.TryToEnable(__instance, true);
            return false;
        }
        return true;
    }
}

/// <summary>
/// Patch to not enable ThermalVision.SwitchComponentsOn if thermal is inoperable (no/drained battery).
/// Called when Thermals are turned on/off.
/// </summary>
public class ThermalVisionOnPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ThermalVision).GetMethod(nameof(ThermalVision.method_1));
    }

    [PatchPrefix]
    protected static bool Prefix(ThermalVision __instance, bool on)
    {
        if (!on) return true;

        ThermalVisionComponent component = GamePlayerOwner.MyPlayer.ThermalVisionObserver.Component;
        if (component is not null && !Singleton<DeviceManager>.Instance.GetIsOperable(component.Item))
        {
            // Skip turning on components, but enable mask
            __instance.TextureMask.TryToEnable(__instance, true);

            // __instance.chromaticAberration_0.Shift = (on ? __instance.ChromaticAberrationThermalShift : __instance.float_0);
            return false;
        }
        return true;
    }
}
