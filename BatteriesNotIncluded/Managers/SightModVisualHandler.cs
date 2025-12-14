using System.Collections.Generic;
using BatteriesNotIncluded.Patches;
using BatteriesNotIncluded.Patches.Sight;
using BatteriesNotIncluded.Utils;
using HarmonyLib;

namespace BatteriesNotIncluded.Managers;

public class SightModVisualHandler
{
    private readonly DeviceManager _deviceManager;
    private readonly HashSet<SightModVisualControllers> _controllers = [];
    private readonly HashSet<SightModVisualControllers> _invalidControllers = [];

    /// <summary>
    /// Key: Device item ID, Value: List of controllers (can be multiple, say UI window renders)
    /// </summary>
    private readonly Dictionary<string, List<SightModVisualControllers>> _itemIdsLookup = [];

    public SightModVisualHandler(DeviceManager manager)
    {
        _deviceManager = manager;
        CaptureSightControllerPatch.OnUpdateSightMode += AddController;
    }

    public void UpdateSightVisibility(string itemId, bool shouldBeActive)
    {
        if (!_itemIdsLookup.TryGetValue(itemId, out var controllers)) return;

        foreach (var controller in controllers)
        {
            UpdateSightVisibilityInternal(controller, shouldBeActive);
        }
    }

    /// <summary>
    ///  Remove null controllers, used OnGameStart.
    /// I'm assuming these are templates of controllers added before the start of the game.
    /// </summary>
    public void RemoveDestroyedControllers()
    {
        var num = _controllers.RemoveWhere((c) => c == null);
        num += _invalidControllers.RemoveWhere((c) => c == null);
        foreach (var (_, value) in _itemIdsLookup)
        {
            value.RemoveAll(c => c == null);
        }
        LoggerUtil.Debug($"Removed {num} null sight controllers");
    }

    public void Cleanup()
    {
        CaptureSightControllerPatch.OnUpdateSightMode -= AddController;
    }

    private void AddController(SightModVisualControllers controller)
    {
        if (IsExisting(controller))
        {
            // LoggerUtil.Debug($"Skipping existing controller for {controller.SightMod.Item.LocalizedShortName()} ({controller.SightMod.Item.Id})"); // Spams
            return;
        }

        var item = controller.SightMod.Item;
        if (item.IsBatteryOperated())
        {
            _controllers.Add(controller);
            AddToLookUp(item.Id, controller);
            LoggerUtil.Debug($"Adding controller for {item.LocalizedShortName()} ({item.Id})");
        }
        else
        {
            _invalidControllers.Add(controller);
            LoggerUtil.Debug($"Adding non-battery operated controller for {item.LocalizedShortName()} ({item.Id})");
        }
    }

    private void AddToLookUp(string itemId, SightModVisualControllers controller)
    {
        if (!_itemIdsLookup.TryGetValue(itemId, out var controllers))
        {
            controllers = [];
            _itemIdsLookup[itemId] = controllers;
        }
        controllers.Add(controller);
    }

    private bool IsExisting(SightModVisualControllers controller) =>
        _controllers.Contains(controller) || _invalidControllers.Contains(controller);

    private static void UpdateSightVisibilityInternal(SightModVisualControllers controller, bool shouldBeActive)
    {
        var scopePrefabCache = _scopePrefabCacheField(controller);
        var scopeModeInfos = _scopeModeInfosField(scopePrefabCache);
        foreach (var scopeModeInfo in scopeModeInfos)
        {
            var collimatorSight = scopeModeInfo.CollimatorSight;
            if (collimatorSight != null)
            {
                collimatorSight.gameObject.SetActive(shouldBeActive);
            }
            var opticSight = scopeModeInfo.OpticSight;
            if (opticSight != null && !shouldBeActive)
            {
                // opticSight.LensFade();
                // opticSight.gameObject.SetActive(false);
                opticSight.enabled = false;

                // TODO: Find a way to only disable the reticle
            }
        }
    }

    private static readonly AccessTools.FieldRef<SightModVisualControllers, ScopePrefabCache> _scopePrefabCacheField =
        AccessTools.FieldRefAccess<SightModVisualControllers, ScopePrefabCache>("scopePrefabCache_0");

    private static readonly AccessTools.FieldRef<ScopePrefabCache, ScopePrefabCache.ScopeModeInfo[]> _scopeModeInfosField =
        AccessTools.FieldRefAccess<ScopePrefabCache, ScopePrefabCache.ScopeModeInfo[]>("_scopeModeInfos");
}
