using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Tactical;

public class GetHeadLightStatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LightComponent).GetMethod(nameof(LightComponent.GetLightState));
    }

    [PatchPostfix]
    protected static void Postfix(LightComponent __instance, bool toggleActive, bool switchMod, ref FirearmLightStateStruct __result)
    {
        if (!toggleActive) return;

        if (!__result.IsActive) return;

        // If player wants light on but has no/drained batteries, don't allow
        var manager = Singleton<DeviceManager>.Instance;
        if (!manager.GetIsOperable(__result.Id))
        {
            __result.IsActive = false;
        }
    }
}

public class SetHeadLightStatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(LightComponent).GetMethod(nameof(LightComponent.SetLightState));
    }

    [PatchPostfix]
    protected static void Postfix(LightComponent __instance, FirearmLightStateStruct state)
    {
        var manager = Singleton<DeviceManager>.Instance;
        manager.ManualUpdate(state.Id);
    }
}
