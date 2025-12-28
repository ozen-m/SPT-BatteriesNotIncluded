using System.Collections.Generic;
using BatteriesNotIncluded.Patches.Sight;
using BatteriesNotIncluded.Utils;
using EFT.CameraControl;
using HarmonyLib;
using UnityEngine;

namespace BatteriesNotIncluded.Managers;

public class SightModVisualHandler
{
    private readonly DeviceManager _deviceManager;
    private readonly HashSet<SightModVisualControllers> _controllers = [];
    private readonly HashSet<SightModVisualControllers> _invalidControllers = [];
    private readonly Dictionary<OpticSight, Texture> _opticSightTextureCache = [];

    /// <summary>
    /// Key: Device item ID, Value: List of controllers (can be multiple, say UI window renders)
    /// </summary>
    private readonly Dictionary<string, List<SightModVisualControllers>> _itemIdsLookup = [];

    public SightModVisualHandler(DeviceManager manager)
    {
        _deviceManager = manager;
        CaptureSightControllerPatch.OnSetSightMode += AddController;
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
    /// Remove null controllers, used OnGameStart.
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
        CaptureSightControllerPatch.OnSetSightMode -= AddController;
    }

    private void AddController(SightModVisualControllers controller)
    {
        if (IsExisting(controller)) return;

        var item = controller.SightMod.Item;
        if (_deviceManager.IsItemRegistered(item))
        {
            _controllers.Add(controller);
            AddToLookUp(item.Id, controller);
        }
        else
        {
            _invalidControllers.Add(controller);
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

    private void UpdateSightVisibilityInternal(SightModVisualControllers controller, bool shouldBeActive)
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
            if (opticSight != null)
            {
                // If it's a thermal/nvg sight, the whole sight.
                if (controller.SightMod.Item is SpecialScopeItemClass)
                {
                    opticSight.LensFade(!shouldBeActive);
                    continue;
                }

                // Cache the reticle texture since we're setting it to null if optic is disabled.
                if (!_opticSightTextureCache.TryGetValue(opticSight, out var texture))
                {
                    texture = opticSight.LensRenderer.sharedMaterial.GetTexture(_markTex);
                    _opticSightTextureCache.Add(opticSight, texture);
                }

                // Only the reticle
                opticSight.LensRenderer.sharedMaterial.SetTexture(_markTex, shouldBeActive ? texture : null);
            }
        }
    }

    private static readonly int _markTex = Shader.PropertyToID("_MarkTex");

    private static readonly AccessTools.FieldRef<SightModVisualControllers, ScopePrefabCache> _scopePrefabCacheField =
        AccessTools.FieldRefAccess<SightModVisualControllers, ScopePrefabCache>("scopePrefabCache_0");

    private static readonly AccessTools.FieldRef<ScopePrefabCache, ScopePrefabCache.ScopeModeInfo[]> _scopeModeInfosField =
        AccessTools.FieldRefAccess<ScopePrefabCache, ScopePrefabCache.ScopeModeInfo[]>("_scopeModeInfos");
}
