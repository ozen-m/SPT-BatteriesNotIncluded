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
    protected static void Postfix(TacticalComboVisualController __instance, bool isYourPlayer)
    {
        if (!ToSkip && Singleton<DeviceManager>.Instantiated)
        {
            Singleton<DeviceManager>.Instance.ManualUpdate(__instance.LightMod.Item);
            // BUG: Above not runs when turning dead bot's device on/off, when using item context menu extended
        }
    }
}
