using System;
using System.Reflection;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.FikaSync.Patches;

/// <summary>
/// Inventory equipment gets changed on death.
/// TODO: Check if you're host (not headless)
/// </summary>
public class CorpseInventoryPatch : ModulePatch
{
    public static event Action<InventoryEquipment> OnCorpseNewInventory;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ObservedPlayer).GetMethod(nameof(ObservedPlayer.SetInventory));
    }

    [PatchPostfix]
    protected static void Postfix(ObservedPlayer __instance)
    {
        OnCorpseNewInventory?.Invoke(__instance.Inventory.Equipment);
    }
}
