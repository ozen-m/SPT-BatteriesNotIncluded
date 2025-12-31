using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Tactical.Bot;

/// <summary>
/// Patch BotLight so that the bot would not continuously try to turn on devices with drained batteries
/// </summary>
public class TurnOnPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BotLight).GetMethod(nameof(BotLight.TurnOn));
    }

    [PatchPostfix]
    public static void Postfix(BotLight __instance)
    {
        if (!__instance.HaveLight) return;

        if (!Singleton<DeviceManager>.Instance.GetIsOperable(__instance.LightMod.Item))
        {
            __instance.HaveLight = false;
        }
    }
}
