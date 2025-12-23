using System.Diagnostics;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public abstract class BaseDelayedSystem(float runInterval) : ISystem
{
    protected readonly float RunInterval = runInterval;
    private readonly Stopwatch _runTimer = Stopwatch.StartNew();

    public virtual void Run(DeviceManager manager)
    {
        if (!CanRun()) return;

        for (var i = 0; i < manager.Devices.Count; i++)
        {
            Run(manager, i);
        }
    }

    public abstract void Run(DeviceManager manager, int i);

    public virtual void ForceRun(DeviceManager manager)
    {
        RestartTimer();
        Run(manager);
    }

    private bool CanRun()
    {
        if (_runTimer.ElapsedMilliseconds < RunInterval) return false;

        _runTimer.Restart();
        return true;
    }

    private void RestartTimer()
    {
        _runTimer.Restart();
    }
}
