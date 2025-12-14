using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public interface ISystem
{
    public void Run(DeviceManager manager);

    public void Run(DeviceManager manager, int i);
}
