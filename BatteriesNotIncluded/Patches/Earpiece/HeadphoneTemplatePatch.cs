using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Earpiece;

/// <summary>
/// Replace headphone template with default if headphone is inoperable/inactive
/// </summary>
public class HeadphoneTemplatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(HeadphonesItemClass).GetMethod("get_Template");
    }

    [PatchPrefix]
    public static bool Prefix(HeadphonesItemClass __instance, ref HeadphonesTemplateClass __result)
    {
        if (!Singleton<DeviceManager>.Instantiated) return true;
        if (!__instance.TryGetItemComponent(out TogglableComponent togglableComponent)) return true;

        var manager = Singleton<DeviceManager>.Instance;
        if (!togglableComponent.On || togglableComponent.On && !manager.GetIsOperable(__instance))
        {
            __result = _default;
            return false;
        }
        return true;
    }

    // TODO: Check for conflicts with Realism mod

    /// <summary>
    /// Copied from GClass2596.Default  
    /// </summary>
    private static readonly HeadphonesTemplateClass _default = new()
    {
        ShortName = "Default",
        GunsCompressorSendLevel = -80f,
        ClientPlayerCompressorSendLevel = -80f,
        ObservedPlayerCompressorSendLevel = -80f,
        NpcCompressorSendLevel = -80f,
        EnvTechnicalCompressorSendLevel = -80f,
        EnvNatureCompressorSendLevel = -80f,
        EnvCommonCompressorSendLevel = -80f,
        AmbientCompressorSendLevel = -80f,
        HeadphonesMixerVolume = -80f,
        EffectsReturnsCompressorSendLevel = -80f,
        DryVolume = 0f,
        AmbientVolume = 0f,
        CompressorAttack = 0f,
        CompressorGain = 0f,
        CompressorRelease = 0f,
        CompressorThreshold = 0f,
        Distortion = 0f,
        HighpassResonance = 1f,
        HighpassFreq = 60,
        LowpassFreq = 22000,
        EffectsReturnsGroupVolume = 0f,
        EQBand1Gain = 1f,
        EQBand2Gain = 1f,
        EQBand3Gain = 1f
    };
}
