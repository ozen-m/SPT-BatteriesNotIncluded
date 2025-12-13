using System;
using System.Reflection;
using BatteriesNotIncluded.Utils;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches;

public class OnItemAddedOrRemovedPatch : ModulePatch
{
    public static event Action<Item> OnItemAddedOrRemoved;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.OnItemAddedOrRemoved));
    }

    [PatchPrefix]
    protected static void Prefix(Player __instance, Item item, ItemAddress location, bool added)
    {
        if (location is GClass3393) return;

#if DEBUG
        if (!__instance.IsYourPlayer) LoggerUtil.Warning($"Player.OnItemAddedOrRemoved patch ran for {__instance.Profile.Nickname}");
#endif
        // In pre for order execution
        OnItemAddedOrRemoved?.Invoke(item);
    }
}
