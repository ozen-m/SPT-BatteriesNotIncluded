using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace BatteriesNotIncluded.Utils;

[Injectable(InjectionType.Singleton)]
public class ApbsCompatibility(IReadOnlyList<SptMod> sptMods, LoggerUtil loggerUtil)
{
    private bool? _dataLoaderFound;
    private Type _dataLoaderType;
    private object _dataLoader;

    public Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>[] GetModsData(int tierCount)
    {
        if (!Init()) return null;

        var tiers = new Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>[tierCount];
        for (var i = 0; i < tierCount; i++)
        {
            var tierMethodInfo = AccessTools.PropertyGetter(_dataLoaderType, $"Tier{i}ModsData");
            tiers[i] = tierMethodInfo?.Invoke(_dataLoader, null) as Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>>;
        }

        return tiers;
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

#pragma warning disable CS0618 // Type or member is obsolete
            _dataLoader = ServiceLocator.ServiceProvider.GetService(_dataLoaderType); // TODO: 4.1.x
#pragma warning restore CS0618 // Type or member is obsolete
            if (_dataLoader == null) continue;

            loggerUtil.Debug("APBS DataLoader found!");
            _dataLoaderFound = true;
            break;
        }

        if (!_dataLoaderFound.Value)
        {
            loggerUtil.Warning($"APBS is installed but cannot find DataLoader, something went wrong");
        }

        return _dataLoaderFound.Value;
    }
}
