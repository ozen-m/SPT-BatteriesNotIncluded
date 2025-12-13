using BatteriesNotIncluded.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace BatteriesNotIncluded.Utils;

[Injectable]
public class LoggerUtil(ISptLogger<BatteriesNotIncluded> logger, ModConfigContainer modConfigContainer)
{
    private const string LogPrefix = "[BatteriesNI] ";

    private ModConfig ModConfig => modConfigContainer.ModConfig;

    public void Success(object message) => logger.Success(LogPrefix + message);

    public void Info(object message) => logger.Info(LogPrefix + message);

    public void Warning(object message) => logger.Warning(LogPrefix + message);

    public void Error(object message) => logger.Error(LogPrefix + message);

    public void Debug(object message)
    {
        if (ModConfig.DebugLogs)
        {
            logger.Debug(LogPrefix + message);
        }
    }
}
