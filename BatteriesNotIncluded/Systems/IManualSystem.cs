using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public interface IManualSystem
{
    public void Run(DeviceManager manager, int i);
}
