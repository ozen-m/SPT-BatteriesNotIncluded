using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches.Headwear;

/// <summary>
/// Do not include headphones togglable component when looking for TogglableComponents in ECommand.ToggleGoggles
/// </summary>
public class TogglableConflictPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.method_15));
    }

    [PatchPrefix]
    public static bool Prefix(Player __instance)
    {
        if (__instance.InventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem is not CompoundItem containedItem)
        {
            return false;
        }

        var togglableComponents = containedItem.GetItemComponentsInChildren<TogglableComponent>();
        foreach (var togglableComponent in togglableComponents)
        {
            if (togglableComponent.Item is HeadphonesItemClass) continue;

            _ = __instance.InventoryController.TryRunNetworkTransaction(togglableComponent.Set(!togglableComponent.On, true));
            return false;
        }
        return false;
    }
}
