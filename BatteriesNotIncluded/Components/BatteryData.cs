namespace BatteriesNotIncluded.Components;

public readonly struct BatteryData(float drainMultiplier)
{
    // public readonly int Slots = slots;
    public readonly float DrainMultiplier = drainMultiplier;
}
