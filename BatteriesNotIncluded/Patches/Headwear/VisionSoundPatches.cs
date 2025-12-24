using System.Reflection;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BatteriesNotIncluded.Patches.Headwear;

/// <summary>
/// Patches sounds to play when toggling NightVisions and ThermalVisions.
/// Should only matter for your player.
/// </summary>
public class PlayNightVisionSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.PlayNightVisionSound));
    }

    [PatchPrefix]
    protected static bool Prefix(Player __instance, AudioClip ___NightVisionOn, AudioClip ___NightVisionOff, Vector3 ___SpeechLocalPosition)
    {
        if (!Singleton<DeviceManager>.Instantiated) return true;
        if (!__instance.IsYourPlayer) return true;

        var manager = Singleton<DeviceManager>.Instance;
        NightVisionComponent component = __instance.NightVisionObserver.Component;
        var shouldPlayOnSound = component != null && (component.Togglable == null || component.Togglable.On && manager.GetIsOperable(component.Item));
        __instance.PlayToggleSound(shouldPlayOnSound ? ___NightVisionOn : ___NightVisionOff, ___SpeechLocalPosition);
        return false;
    }
}

public class PlayThermalVisionSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.PlayThermalVisionSound));
    }

    [PatchPrefix]
    protected static bool Prefix(Player __instance, AudioClip ___ThermalVisionOn, AudioClip ___ThermalVisionOff, Vector3 ___SpeechLocalPosition)
    {
        if (!Singleton<DeviceManager>.Instantiated) return true;
        if (!__instance.IsYourPlayer) return true;

        var manager = Singleton<DeviceManager>.Instance;
        ThermalVisionComponent component = __instance.ThermalVisionObserver.Component;
        var shouldPlayOnSound = component != null && (component.Togglable == null || component.Togglable.On && manager.GetIsOperable(component.Item));
        __instance.PlayToggleSound(shouldPlayOnSound ? ___ThermalVisionOn : ___ThermalVisionOff, ___SpeechLocalPosition);
        return false;
    }
}
