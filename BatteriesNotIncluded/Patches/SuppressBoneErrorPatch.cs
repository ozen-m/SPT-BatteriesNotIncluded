using System;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.Patches;

public class SuppressBoneErrorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(LoggerClass), nameof(LoggerClass.LogError));
    }

    [PatchPrefix]
    public static bool PatchPrefix(string format)
    {
        if (format.StartsWith("bone mod_equipment_00", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}
