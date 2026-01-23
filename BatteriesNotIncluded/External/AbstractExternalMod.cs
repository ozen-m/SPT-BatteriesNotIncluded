using System;
using BepInEx;
using BepInEx.Bootstrap;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.External;

public abstract class AbstractExternalMod
{
    protected abstract string Guid { get; }
    public virtual Version MinimumVersion { get; } = new(0, 0, 0);
    public virtual Version MaximumVersion { get; } = new(999, 999, 999);

    public PluginInfo PluginInfo { get; private set; }
    public bool IsPresent => PluginInfo is not null;
    public bool IsInitialized { get; protected set; }
    public string ErrorMessage { get; protected set; } = string.Empty;

    protected abstract ModulePatch[] Patches { get; }

    public bool CheckIfPresent()
    {
        var installed = Chainloader.PluginInfos.TryGetValue(Guid, out var pluginInfo);
        PluginInfo = pluginInfo;

        return installed;
    }

    public virtual bool IsCompatible() => PluginInfo?.Metadata.Version >= MinimumVersion &&
                                          PluginInfo?.Metadata.Version <= MaximumVersion;

    public virtual bool TryToInitialize()
    {
        if (!TryToReflect())
        {
            return false;
        }

        IsInitialized = EnablePatches();
        if (!IsInitialized)
        {
            // Some patches can get enabled when failing
            DisablePatches();
        }

        return IsInitialized;
    }

    protected virtual bool TryToReflect() => true;

    private bool EnablePatches()
    {
        try
        {
            foreach (var patch in Patches)
            {
                patch.Enable();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            return false;
        }
        return true;
    }

    public void DisablePatches()
    {
        try
        {
            foreach (var patch in Patches)
            {
                patch.Disable();
            }
        }
        catch (Exception)
        {
            // Ignored
        }
    }
}
