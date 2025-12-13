using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Sight;

/// <summary>
/// Run manual systems on aim down sight
/// </summary>
public class AimDownSightPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Method is technically to play aim down sound
        // Sidenote: wow 60th method
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.method_60));
    }

    [PatchPostfix]
    protected static void Postfix(Player.FirearmController __instance)
    {
        var manager = Singleton<DeviceManager>.Instance;
        manager.RunManualUpdateNextFrame();
    }
}
