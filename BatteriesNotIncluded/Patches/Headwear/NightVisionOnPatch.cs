using System.Reflection;
using BatteriesNotIncluded.Managers;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Headwear;

/// <summary>
/// Do not enable NightVision.SwitchComponentsOn if nvg is inoperable (no/drained battery).
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

        var manager = Singleton<DeviceManager>.Instance;
        NightVisionComponent component = GamePlayerOwner.MyPlayer.NightVisionObserver.Component;
        if (component is not null && !manager.GetIsActive(component.Item.Id))
        {
            // Skip turning on components, but enable mask
            __instance.TextureMask.TryToEnable(__instance, true);
            return false;
        }
        return true;
    }
}
