using System.Collections.Generic;
using BatteriesNotIncluded.Patches.Sight;
using BatteriesNotIncluded.Utils;
using EFT.CameraControl;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace BatteriesNotIncluded.Managers;

public class SightModVisualHandler
{
    private readonly DeviceManager _manager;
    private readonly HashSet<SightModVisualControllers> _controllers = [];
    private readonly Dictionary<OpticSight, Texture> _opticSightTextureCache = [];

    public SightModVisualHandler(DeviceManager manager)
    {
        _manager = manager;
        CaptureSightControllerPatch.OnSetSightMode += AddController;
    }

    public void UpdateSightVisibility(Item item, bool shouldBeActive)
    {
        foreach (var controller in _controllers)
        {
            if (controller.SightMod.Item != item) continue;

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
        LoggerUtil.Debug($"Removed {num} null sight controllers");
    }

    public void Cleanup()
    {
        CaptureSightControllerPatch.OnSetSightMode -= AddController;
    }

    private void AddController(SightModVisualControllers controller)
    {
        if (_manager.IsItemRegistered(controller.SightMod.Item))
        {
            _controllers.Add(controller);
        }
    }

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

                // Only the reticle.
                // Cache the reticle texture since we're setting it to null if optic is disabled.
                if (!_opticSightTextureCache.TryGetValue(opticSight, out var texture))
                {
                    texture = opticSight.LensRenderer.sharedMaterial.GetTexture(_markTex);
                    _opticSightTextureCache.Add(opticSight, texture);
                }
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
