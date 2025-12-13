using System.Diagnostics;

namespace BatteriesNotIncluded.Utils;

public static class LoggerUtil
{
    // [Conditional("DEBUG")]
    public static void Debug(string msg)
    {
        if (BatteriesNotIncluded.DebugLogs.Value)
        {
            BatteriesNotIncluded.LogSource.LogDebug(msg);
        }
    }

    public static void Error(string msg)
    {
        BatteriesNotIncluded.LogSource.LogError(msg);
    }

    public static void Warning(string msg)
    {
        BatteriesNotIncluded.LogSource.LogWarning(msg);
    }
}
