namespace BatteriesNotIncluded.Models;

[Flags]
public enum DeviceMode
{
    None = 0,
    Flashlight = 1,
    VisibleLaser = 2,
    IRFlood = 4,
    IRLaser = 8,
}
