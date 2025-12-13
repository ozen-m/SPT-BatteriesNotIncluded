using System.Diagnostics;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public abstract class BaseDelayedSystem(int _runInterval) : ISystem
{
    private readonly Stopwatch _runTimer = Stopwatch.StartNew();

    public abstract void Run(DeviceManager manager);

    public virtual void ForceRun(DeviceManager manager)
    {
        RestartTimer();
        Run(manager);
    }

    protected bool CanRun()
    {
        if (_runTimer.ElapsedMilliseconds <= _runInterval) return false;

        _runTimer.Restart();
        return true;
    }

    private void RestartTimer()
    {
        _runTimer.Restart();
    }
}
