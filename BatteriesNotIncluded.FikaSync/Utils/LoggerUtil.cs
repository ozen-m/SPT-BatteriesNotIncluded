using System.Diagnostics;

namespace BatteriesNotIncluded.FikaSync.Utils;

public static class LoggerUtil
{
    [Conditional("DEBUG")]
    public static void Debug(string msg)
    {
        BatteriesNotIncludedSync.LogSource.LogDebug(msg);
    }

    public static void Error(string msg)
    {
        BatteriesNotIncludedSync.LogSource.LogError(msg);
    }

    public static void Warning(string msg)
    {
        BatteriesNotIncludedSync.LogSource.LogWarning(msg);
    }

    public static void Info(string msg)
    {
        BatteriesNotIncludedSync.LogSource.LogInfo(msg);
    }
}
