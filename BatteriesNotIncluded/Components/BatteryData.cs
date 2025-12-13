namespace BatteriesNotIncluded.Components;

public readonly struct BatteryData(float drainMultiplier)
{
    public readonly float DrainMultiplier = drainMultiplier;
}
