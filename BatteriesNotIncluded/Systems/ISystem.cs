using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public interface ISystem
{
    public void Run(DeviceManager manager);

    // TODO: Implement run for specific index?
}
