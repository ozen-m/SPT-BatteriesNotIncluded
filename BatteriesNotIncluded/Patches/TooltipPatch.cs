using System.Reflection;
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
        if (!BatteriesNotIncluded.GetDeviceData(__instance.Item.StringTemplateId, out var data)) return;

        var count = 0;
        var min = float.MaxValue;
        foreach (var slot in ((CompoundItem)__instance.Item).Slots)
        {
            if (slot.ContainedItem is null || !slot.ContainedItem.TryGetItemComponent(out ResourceComponent resource)) continue;

            count++;
            var val = resource.Value;
            if (val < min)
            {
                min = val;
            }
        }

        if (count == data.SlotCount && min > 0f)
        {
            __result = $"{__result} (~{(int)(min / data.DrainPerSecond / 60f)}m left)";
        }
    }
}
