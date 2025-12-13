using System.Reflection;
using BatteriesNotIncluded.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;

namespace BatteriesNotIncluded.Utils;

[Injectable(InjectionType.Singleton)]
public class ModConfigContainer
{
    public ModConfig ModConfig { get; }
    
    public string ConfigPath { get; }

    public ModConfigContainer(ISptLogger<BatteriesNotIncluded> logger, ModHelper modHelper)
    {
        string modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        ConfigPath = Path.Combine(modPath, "config");

        try
        {
            ModConfig = modHelper.GetJsonDataFromFile<ModConfig>(ConfigPath, "config.jsonc");
        }
        catch (Exception ex)
        {
            ModConfig = new ModConfig();
            logger.Error(ex.ToString());
            logger.Error("[Batteries Not Included] Exception while trying to load configuration file, disabling mod.");
        }
    }
}
