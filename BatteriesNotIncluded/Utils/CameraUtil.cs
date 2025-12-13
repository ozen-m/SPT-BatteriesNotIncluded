namespace BatteriesNotIncluded.Utils;

public static class CameraUtil
{
    public static void ToggleThermalVision() => CameraClass.Instance.ThermalVision.On = !CameraClass.Instance.ThermalVision.On;

    public static void SetThermalVision(bool state) => CameraClass.Instance.ThermalVision.On = state;

    public static void ToggleNightVision() => CameraClass.Instance.NightVision.On = !CameraClass.Instance.NightVision.On;

    public static void SetNightVision(bool state) => CameraClass.Instance.NightVision.On = state;

    public static bool NightVisionInProcessSwitching => CameraClass.Instance.NightVision.InProcessSwitching;

    public static bool ThermalVisionInProcessSwitching => CameraClass.Instance.ThermalVision.InProcessSwitching;
}
