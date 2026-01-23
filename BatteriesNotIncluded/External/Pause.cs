using System;
using System.Reflection;
using BatteriesNotIncluded.Managers;
using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.External;

public class Pause : AbstractExternalMod
{
    protected override string Guid { get; } = "com.netVnum.pause";
    public override Version MinimumVersion { get; } = new(1, 4, 0);
    public override Version MaximumVersion { get; } = new(1, 4, 999);

    protected override ModulePatch[] Patches { get; } =
    [
        new PausePatch(),
        new UnpausePatch()
    ];

    // Reflection
    private static MethodInfo _pauseMethod;
    private static MethodInfo _unpauseMethod;

    public override bool TryToInitialize()
    {
        if (!ExternalMod.Fika.IsPresent) return base.TryToInitialize();

        ErrorMessage = $"{PluginInfo} is not compatible with {ExternalMod.Fika.PluginInfo}";
        return false;
    }

    protected override bool TryToReflect()
    {
        try
        {
            var pauseControllerType = AccessTools.TypeByName("Pause.PauseController");
            _pauseMethod = AccessTools.Method(pauseControllerType, "Pause");
            _unpauseMethod = AccessTools.Method(pauseControllerType, "Unpause");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            return false;
        }
        return true;
    }

    #region Patches
    /// <summary>
    /// Disables device manager to stop draining devices when game is paused
    /// </summary>
    [IgnoreAutoPatch]
    public class PausePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return _pauseMethod;
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            var manager = Singleton<DeviceManager>.Instance;
            if (manager == null) return;

            manager.enabled = false;
        }
    }

    /// <summary>
    /// Enables device manager to continue draining devices when game is unpaused
    /// </summary>
    [IgnoreAutoPatch]
    public class UnpausePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return _unpauseMethod;
        }

        [PatchPostfix]
        protected static void Postfix()
        {
            var manager = Singleton<DeviceManager>.Instance;
            if (manager == null) return;

            manager.enabled = true;
        }
    }
    #endregion
}
