using System.Reflection;
using BatteriesNotIncluded.Utils;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Debug;

public class UpdatePhonesReallyPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.UpdatePhonesReally));
    }

    [PatchPostfix]
    protected static void Postfix(Player __instance)
    {
        LoggerUtil.Debug("UpdatePhonesReally ran");
    }
}
