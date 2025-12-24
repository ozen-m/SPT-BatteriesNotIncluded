using System.Reflection;
using BatteriesNotIncluded.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BatteriesNotIncluded.Patches;

/// <summary>
/// This suppresses warnings in the client logs about bones not being found in mod slots.
/// This breaks something, disabled.
/// </summary>
[IgnoreAutoPatch]
public class AddFakeBonePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PoolManagerClass).GetMethod(nameof(PoolManagerClass.method_3));
    }

    [PatchPrefix]
    protected static void Prefix(GClass3248 containerCollection, GClass768 collectionView)
    {
        if (containerCollection.IsBatteryOperated())
        {
            CreateFakeBone(collectionView.GameObject.transform);
        }
    }

    private static void CreateFakeBone(Transform parent)
    {
        // TODO: Get slot count and add all slots
        var anchor = parent.Find("mod_equipment_000");
        if (anchor != null)
        {
            return;
        }

        var gameObject = new GameObject("mod_equipment_000");

        var transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
    }
}
