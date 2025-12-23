using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches;

public class TooltipPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GridItemView).GetMethod(nameof(GridItemView.method_26));
    }

    [PatchPostfix]
    protected static void Postfix(GridItemView __instance, ref string __result)
    {
        if (!BatteriesNotIncluded.ShowRemainingBattery.Value) return;
        if (!Singleton<DeviceManager>.Instantiated) return;
        if (__instance.Item is not CompoundItem compoundItem) return;
        if (!BatteriesNotIncluded.GetDeviceData(compoundItem.StringTemplateId, out var data)) return;
        if (!Singleton<DeviceManager>.Instance.GetIsOperable(compoundItem)) return;

        float min = float.MaxValue;
        foreach (var slot in compoundItem.Slots)
        {
            if (slot.ContainedItem is null || !slot.ContainedItem.TryGetItemComponent(out ResourceComponent resource)) continue;

            var val = resource.Value;
            if (val < min)
            {
                min = val;
            }
        }

        if (min > 0f)
        {
            __result = $"{__result} (~{(int)(min / data.DrainPerSecond / 60f)}m left)";
        }
    }
}
