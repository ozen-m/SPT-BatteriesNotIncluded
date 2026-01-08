using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace BatteriesNotIncluded.Utils;

[Injectable(InjectionType.Singleton)]
public class ApbsCompatibility(IReadOnlyList<SptMod> sptMods, LoggerUtil loggerUtil, IServiceProvider serviceProvider)
{
    private bool? _dataLoaderFound;
    private Type _dataLoaderType;
    private object _dataLoader;

    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>[] GetModsData(int tierCount)
    {
        if (!Init()) return null;

        var modTiers = new Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>[tierCount];
        for (var i = 0; i < tierCount; i++)
        {
            var modTierMethod = AccessTools.PropertyGetter(_dataLoaderType, $"Tier{i}ModsData");
            if (modTierMethod is null)
            {
                loggerUtil.Warning($"[APBS Compatibility] Tier{i}ModsData cannot be found");
                modTiers[i] = null;
                continue;
            }

            var modTier = modTierMethod.Invoke(_dataLoader, null) as Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>;
            if (modTier is null)
            {
                loggerUtil.Warning($"[APBS Compatibility] Tier{i}ModsData returned null");
            }
            modTiers[i] = modTier;
        }

        return modTiers;
    }

    private bool Init()
    {
        if (_dataLoaderFound.HasValue) return _dataLoaderFound.Value;

        _dataLoaderFound = false;
        var apbsMod = sptMods.FirstOrDefault(m => m.ModMetadata.ModGuid == "com.acidphantasm.progressivebotsystem");
        if (apbsMod is null) return _dataLoaderFound.Value;

        foreach (var assembly in apbsMod.Assemblies)
        {
            _dataLoaderType = assembly.GetType("_progressiveBotSystem.Helpers.DataLoader");
            if (_dataLoaderType == null) continue;

            _dataLoader = serviceProvider.GetService(_dataLoaderType);
            if (_dataLoader == null) continue;

            loggerUtil.Debug("[APBS Compatibility] DataLoader found");
            _dataLoaderFound = true;
            break;
        }

        if (!_dataLoaderFound.Value)
        {
            loggerUtil.Warning($"[APBS Compatibility] APBS is installed but cannot find DataLoader, something went wrong");
        }

        return _dataLoaderFound.Value;
    }
}
