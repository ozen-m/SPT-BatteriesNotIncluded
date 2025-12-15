using System.Reflection;
using BatteriesNotIncluded.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BatteriesNotIncluded.Patches;

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
        // Is this expensive?
        // TODO: Get slot count and add all slots
        var anchor = parent.Find("mod_equipment_000");
        if (anchor != null)
        {
            return;
        }

        var gameObject = new GameObject("mod_equipment_000");

        var transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}
