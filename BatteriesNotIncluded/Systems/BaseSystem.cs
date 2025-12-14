using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public abstract class BaseSystem : ISystem
{
    public virtual void Run(DeviceManager manager)
    {
        for (var i = 0; i < manager.Devices.Count; i++)
        {
            Run(manager, i);
        }
    }

    public abstract void Run(DeviceManager manager, int i);
}
