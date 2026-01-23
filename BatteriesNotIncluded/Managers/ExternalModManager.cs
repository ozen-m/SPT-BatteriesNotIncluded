using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Utils;

namespace BatteriesNotIncluded.Managers;

/// <summary>
/// Based on DanW's ExternalModHandler, thanks DanW!
/// </summary>
public static class ExternalMod
{
    public static readonly Fika Fika = new();
    public static readonly FikaSync FikaSync = new();
    public static readonly FpvDrone FpvDrone = new();
    public static readonly Pause Pause = new();

    private static readonly AbstractExternalMod[] _externalMods =
    [
        Fika,
        FikaSync,
        FpvDrone,
        Pause
    ];

    public static void CheckForExternalMods()
    {
        foreach (var externalMod in _externalMods)
        {
            if (!externalMod.CheckIfPresent()) continue;

            LoggerUtil.Info($"Found external mod: {externalMod.PluginInfo}");

            if (!externalMod.IsCompatible())
            {
                LoggerUtil.Warning($"{externalMod.PluginInfo.Metadata.GUID} detected with incompatible version {externalMod.PluginInfo.Metadata.Version} (Min: {externalMod.MinimumVersion} Max: {externalMod.MaximumVersion})");
                continue;
            }

            if (!externalMod.TryToInitialize())
            {
                LoggerUtil.Warning($"{externalMod.PluginInfo} compatibility could not be initialized\n{externalMod.ErrorMessage}");
            }
        }
    }

    public static void DisablePatches()
    {
        foreach (var externalMod in _externalMods)
        {
            externalMod.DisablePatches();
        }
    }
}
