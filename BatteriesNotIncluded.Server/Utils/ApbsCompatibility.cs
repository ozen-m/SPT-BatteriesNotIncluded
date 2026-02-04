using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;

namespace BatteriesNotIncluded.Utils;

[Injectable(InjectionType.Singleton)]
public class ApbsCompatibility(IReadOnlyList<SptMod> sptMods, LoggerUtil loggerUtil, IServiceProvider serviceProvider)
{
    private readonly Version _minVersion = new(2, 1, 0);
    private readonly Version _maxVersion = new(2, 1, 999);

    private bool? _getTierModsDataFound;
    private Func<int, Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>> _getTierModsData;

    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>[] GetModsData(int tierCount)
    {
        if (!Init()) return null;

        var modTiers = new Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>[tierCount];
        for (var i = 0; i < tierCount; i++)
        {
            try
            {
                var modTier = _getTierModsData(i);
                if (modTier is null)
                {
                    loggerUtil.Warning($"[APBS Compatibility] Mods data for tier {i} is null");
                }
                modTiers[i] = modTier;
            }
            catch (Exception)
            {
                loggerUtil.Warning($"[APBS Compatibility] Mods data for tier {i} is invalid");
            }
        }

        return modTiers;
    }

    private bool Init()
    {
        if (_getTierModsDataFound.HasValue) return _getTierModsDataFound.Value;

        _getTierModsDataFound = false;
        var apbsMod = sptMods.FirstOrDefault(m => m.ModMetadata.ModGuid == "com.acidphantasm.progressivebotsystem");
        if (apbsMod is null) return _getTierModsDataFound.Value;

        if (apbsMod.ModMetadata.Version < _minVersion || apbsMod.ModMetadata.Version > _maxVersion)
        {
            loggerUtil.Warning($"[APBS Compatibility] APBS found with incompatible version {apbsMod.ModMetadata.Version}, supported versions {_minVersion} - {_maxVersion}");
            return _getTierModsDataFound.Value;
        }

        foreach (var assembly in apbsMod.Assemblies)
        {
            var botConfigHelperType = assembly.GetType("_progressiveBotSystem.Helpers.BotConfigHelper");
            if (botConfigHelperType == null) continue;

            var botConfigHelper = serviceProvider.GetService(botConfigHelperType);
            if (botConfigHelper == null) continue;

            var getTierModsDataMethod = AccessTools.Method(botConfigHelperType, "GetTierModsData");
            if (getTierModsDataMethod == null) continue;

            _getTierModsData =
                AccessTools.MethodDelegate<Func<int, Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>>>(
                    getTierModsDataMethod,
                    botConfigHelper
                );
            if (_getTierModsData == null) continue;

            _getTierModsDataFound = true;
            loggerUtil.Debug("[APBS Compatibility] GetTierModsData found");
            break;
        }

        if (!_getTierModsDataFound.Value)
        {
            loggerUtil.Warning($"[APBS Compatibility] APBS is installed but cannot find GetTierModsData, something went wrong");
        }

        return _getTierModsDataFound.Value;
    }
}
