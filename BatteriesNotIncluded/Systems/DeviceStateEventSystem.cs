using System;
using BatteriesNotIncluded.Managers;

namespace BatteriesNotIncluded.Systems;

public class DeviceStateEventSystem : BaseSystem
{
    /// <summary>
    /// Fika event hook: DeviceId, IsOperable, IsActive
    /// </summary>
    public event Action<string, bool, bool> OnDeviceStateChanged;

    public override void Run(DeviceManager manager, int i)
    {
        var isOperable = manager.IsOperable[i];
        var isActive = manager.IsActive[i];
        var stateChanged = manager.IsPrevOperable[i] != isOperable || manager.IsPrevActive[i] != isActive;
        if (stateChanged)
        {
            OnDeviceStateChanged?.Invoke(manager.Devices[i].Id, isOperable, isActive);
        }
    }
}
