using System.Collections.Generic;
using BatteriesNotIncluded.Patches.Sight;
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

    public void SetSightVisibility(Item item, bool shouldBeActive)
    {
        foreach (var controller in _controllers)
        {
            if (controller.SightMod.Item != item) continue;

            SetSightVisibilityInternal(controller, shouldBeActive);
        }
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

    private void SetSightVisibilityInternal(SightModVisualControllers controller, bool shouldBeActive)
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
