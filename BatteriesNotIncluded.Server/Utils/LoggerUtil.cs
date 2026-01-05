using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace BatteriesNotIncluded.Utils;

[Injectable]
public class LoggerUtil(ISptLogger<BatteriesNotIncluded> logger, ConfigUtil configUtil)
{
    private const string LogPrefix = "[BatteriesNI] ";

    public void Success(object message) => logger.Success(LogPrefix + message);

    public void Info(object message) => logger.Info(LogPrefix + message);

    public void Warning(object message) => logger.Warning(LogPrefix + message);

    public void Error(object message) => logger.Error(LogPrefix + message);

    public void Debug(object message)
    {
        if (configUtil.ModConfig.DebugLogs)
        {
            logger.Debug(LogPrefix + message);
        }
    }
}
