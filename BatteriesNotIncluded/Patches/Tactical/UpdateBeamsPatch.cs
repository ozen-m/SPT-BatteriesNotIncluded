using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Tactical;

public class UpdateBeamsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TacticalComboVisualController).GetMethod(nameof(TacticalComboVisualController.UpdateBeams));
    }

    [PatchPostfix]
    protected static void Postfix(TacticalComboVisualController __instance, bool isYourPlayer)
    {
        if (Singleton<DeviceManager>.Instantiated)
        {
            Singleton<DeviceManager>.Instance.ManualUpdate(__instance.LightMod.Item);
        }
    }
}
