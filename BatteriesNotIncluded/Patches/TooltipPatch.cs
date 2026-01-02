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
    public static void Postfix(GridItemView __instance, ref string __result)
    {
        if (!BatteriesNotIncluded.ShowRemainingBattery.Value) return;
        if (!BatteriesNotIncluded.GetDeviceData(__instance.Item.TemplateId, out var data)) return;

        float drainPerSecond;
        if (__instance.Item is TacticalComboItemClass)
        {
            // Tactical devices drain is mode dependent
            if (!Singleton<DeviceManager>.Instantiated) return;

            var manager = Singleton<DeviceManager>.Instance;
            var index = manager.GetItemIndex(__instance.Item.Id);
            if (index == -1) return;

            drainPerSecond = manager.DrainPerSecond[index];
        }
        else
        {
            drainPerSecond = data.DrainPerSecond;
        }

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
            __result = $"{__result} {string.Format("tooltip-remaining".Localized(), (int)(min / drainPerSecond / 60f))}";
        }
    }
}
