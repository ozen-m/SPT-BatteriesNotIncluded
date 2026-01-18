using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Tactical;

public class UpdateBeamsPatch : ModulePatch
{
    public static bool ToSkip { get; set; }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(TacticalComboVisualController).GetMethod(nameof(TacticalComboVisualController.UpdateBeams));
    }

    [PatchPostfix]
    public static void Postfix(TacticalComboVisualController __instance, bool isYourPlayer)
    {
        if (ToSkip) return;

        var lightMod = __instance.LightMod;
        if (lightMod == null) return;

        var manager = Singleton<DeviceManager>.Instance;
        if (manager == null) return;

        manager.ManualUpdate(lightMod.Item);
        manager.UpdateDeviceMode(__instance);

        // BUG: Above not runs when turning dead bot's device on/off using item context menu extended
    }
}
