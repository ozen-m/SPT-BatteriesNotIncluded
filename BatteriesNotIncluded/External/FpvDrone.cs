using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Systems;
using BatteriesNotIncluded.Utils;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BatteriesNotIncluded.External;

public class FpvDrone : AbstractExternalMod
{
    protected override string Guid { get; } = "com.pein.fpvdronemod";
    public override Version MinimumVersion { get; } = new(0, 5, 0);
    public override Version MaximumVersion { get; } = new(0, 5, 0);

    protected override ModulePatch[] Patches { get; } =
    [
        new CanPilotDronePatch(),
        new GetFailReasonStringPatch(),
        new StartPatch(),
        new OnPilotEnterPatch(),
        new OnPilotExitPatch(),
        new DroneControllerUpdate(),
    ];

    public override bool TryToInitialize() => TryToReflect() && base.TryToInitialize();

    // Update timers
    private const long BatterySlotUpdateInterval = 1000L;
    private static readonly Stopwatch _updateStopwatch = Stopwatch.StartNew();
    private static bool _isPiloting;

    // Reflection
    private static Type _droneHelperType;
    private static Type _baseDroneControllerType;
    private static AccessTools.FieldRef<object, float> _maxBattery;
    private static AccessTools.FieldRef<object, float> _batteryRemaining;
    private static AccessTools.FieldRef<object, object> _droneInput;
    private static MethodInfo _controlDroneMethod;

    protected override bool TryToReflect()
    {
        try
        {
            _droneHelperType = AccessTools.TypeByName("FPVDroneModClient.Helpers.DroneHelper");
            _baseDroneControllerType = AccessTools.TypeByName("FPVDroneModClient.Components.Base.BaseDroneController");
            _maxBattery = AccessTools.FieldRefAccess<float>(_baseDroneControllerType, "MaxBattery");
            _batteryRemaining = AccessTools.FieldRefAccess<float>(_baseDroneControllerType, "BatteryRemaining");
            _droneInput = AccessTools.FieldRefAccess<object>(_baseDroneControllerType, "DroneInput");
            _controlDroneMethod = AccessTools.Method("FPVDroneModClient.Helpers.DroneHelper:ControlDrone");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            return false;
        }
        return true;
    }

    #region Patches
    /// <summary>
    /// Do not allow to pilot the drone if conditions are not met
    /// </summary>
    [IgnoreAutoPatch]
    public class CanPilotDronePatch : ModulePatch
    {
        public static Weapon CurrentRadioController { get; private set; }
        public static Item CurrentGoggles { get; private set; }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_droneHelperType, "CanPilotDrone");
        }

        [PatchPostfix]
        protected static void Postfix(ref bool __result, ref object failReason)
        {
            if (!__result) return;

            var manager = Singleton<DeviceManager>.Instance;
            if (manager == null) return;

            // Radio Controller
            var currentWeapon =
                Singleton<GameWorld>.Instance.MainPlayer.HandsController is Player.FirearmController firearmController
                    ? firearmController.Weapon
                    : null;
            if (currentWeapon is not null)
            {
                CurrentRadioController = currentWeapon;
                manager.ManualUpdate(currentWeapon);

                var operable = manager.GetIsOperable(currentWeapon);
                if (!operable)
                {
                    var enumType = failReason.GetType();
                    failReason = Enum.ToObject(enumType, 2 /* EDronePilotFailReason.NoController */);
                    __result = false;
                    return;
                }
            }

            // Goggles
            var eyewearSlot = Singleton<GameWorld>.Instance.MainPlayer.Inventory.Equipment.GetSlot(EquipmentSlot.Eyewear);
            if (eyewearSlot?.ContainedItem is { } goggles)
            {
                CurrentGoggles = goggles;
                manager.ManualUpdate(goggles);

                var operable = manager.GetIsOperable(goggles);
                if (!operable)
                {
                    var enumType = failReason.GetType();
                    failReason = Enum.ToObject(enumType, 1 /* EDronePilotFailReason.NoHelmet */);
                    __result = false;
                }
            }
        }
    }

    /// <summary>
    /// Set fail message sent to player
    /// </summary>
    [IgnoreAutoPatch]
    public class GetFailReasonStringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_droneHelperType, "GetFailReasonString");
        }

        [PatchPostfix]
        protected static void Postfix(ref string __result, object failReason)
        {
            switch (Convert.ToInt32(failReason))
            {
                // EDronePilotFailReason.NoController 
                case 2:
                    __result = "Controller has missing or drained batteries";
                    return;
                // EDronePilotFailReason.NoHelmet 
                case 1:
                    __result += " or has missing/drained batteries";
                    return;
            }
        }
    }

    /// <summary>
    /// Sets the remaining battery of the drone
    /// </summary>
    [IgnoreAutoPatch]
    public class StartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_baseDroneControllerType, "Start");
        }

        [PatchPostfix]
        protected static void Postfix(object __instance)
        {
            SetDroneRemainingBattery(__instance);
        }
    }

    /// <summary>
    /// Sets the remaining battery of the drone
    /// </summary>
    [IgnoreAutoPatch]
    public class OnPilotEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_baseDroneControllerType, "OnPilotEnter");
        }

        [PatchPostfix]
        protected static void Postfix(object __instance)
        {
            SetDroneRemainingBattery(__instance);

            SetDroneDevicesActiveState(true);
            _isPiloting = true;
        }
    }

    [IgnoreAutoPatch]
    public class OnPilotExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_baseDroneControllerType, "OnPilotExit");
        }

        [PatchPostfix]
        protected static void Postfix(object __instance)
        {
            SetDroneDevicesActiveState(false);
            _isPiloting = false;
        }
    }

    /// <summary>
    /// Updates the drone's batteries (battery slots) resource value and enforce remote controls
    /// </summary>
    [IgnoreAutoPatch]
    public class DroneControllerUpdate : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(_baseDroneControllerType, "Update");
        }

        [PatchPostfix]
        protected static void Postfix(object __instance)
        {
            if (!_isPiloting || _updateStopwatch.ElapsedMilliseconds <= BatterySlotUpdateInterval) return;

            _updateStopwatch.Restart();

            // Drone batteries to drone slot batteries
            UpdateBatteriesInSlots(__instance);

            // Check if still operable
            var manager = Singleton<DeviceManager>.Instance;
            if (manager == null) return;

            EnforceRadioController(__instance, manager);
            EnforceGoggles(manager);
        }

        /// <summary>
        /// Set resource value of batteries in device battery slots based on the remaining battery of the drone.
        /// Resource value is averaged based on battery count.
        /// </summary>
        private static void UpdateBatteriesInSlots(object droneControllerObj)
        {
            var batteryCount = 0;
            var batterySlots = droneControllerObj.GetOrCreateBatterySlots();

            foreach (var batterySlot in batterySlots)
            {
                if (batterySlot.ContainedItem is null) continue;

                batteryCount++;
            }
            var factor = _maxBattery(droneControllerObj) / 100f;
            var chargePerBattery = _batteryRemaining(droneControllerObj) / factor * batterySlots.Length / batteryCount;
            // Technically impossible to happen irl, but averaging is the most feasible way. Or maybe we do weighted calculations...

            foreach (var batterySlot in batterySlots)
            {
                if (batterySlot.ContainedItem is not { } batteryItem) continue;

                if (!batteryItem.TryGetItemComponent(out ResourceComponent resourceComponent))
                {
                    LoggerUtil.Warning("Found battery in drone but no resource component!");
                    continue;
                }

                resourceComponent.Value = chargePerBattery;
#if DEBUG
                LoggerUtil.Info($"Set resource value of battery to {resourceComponent.Value}");
#endif
            }
        }

        private static void EnforceRadioController(object droneControllerObj, DeviceManager manager)
        {
            var controller = CanPilotDronePatch.CurrentRadioController;
            if (controller is null) return;

            var index = manager.GetItemIndex(controller);
            if (index == -1) return;

            var droneInput = (MonoBehaviour)_droneInput(droneControllerObj);
            if (droneInput == null) return; // Shouldn't really be null

            var controllerBatterySlots = manager.BatterySlots[index];
            var isDeviceOperable = DeviceOperableSystem.IsDeviceOperable(controllerBatterySlots);
            if (!isDeviceOperable && droneInput.enabled)
            {
                // Radio controller ran out of batteries
                // Set input to 0f so the drone doesn't go flying into the distance
                var droneInputType = AccessTools.TypeByName("FPVDroneModClient.Components.Drone.DroneInput");
                AccessTools.Field(droneInputType, "ThrottleInput").SetValue(droneInput, 0f);
                AccessTools.Field(droneInputType, "PitchInput").SetValue(droneInput, 0f);
                AccessTools.Field(droneInputType, "YawInput").SetValue(droneInput, 0f);
                AccessTools.Field(droneInputType, "RollInput").SetValue(droneInput, 0f);
                AccessTools.Field(droneInputType, "AltitudeInput").SetValue(droneInput, 0f);
                AccessTools.Field(droneInputType, "CameraPitchInput").SetValue(droneInput, 0f);
                AccessTools.Field(droneInputType, "CameraZoomInput").SetValue(droneInput, 0f);
                droneInput.enabled = false;

                // We disabled input, so we have to start our own and listen for exit
                droneInput.StartCoroutine(ExitDroneKeyListener());

                NotificationManagerClass.DisplayWarningNotification(
                    $"{controller.LocalizedName()} ran out of batteries",
                    ENotificationDurationType.Long
                );
            }
            else if (isDeviceOperable && !droneInput.enabled)
            {
                // Placed fresh batteries
                droneInput.enabled = true;
            }
        }

        private static void EnforceGoggles(DeviceManager manager)
        {
            var goggles = CanPilotDronePatch.CurrentGoggles;
            if (goggles is null) return;

            var index = manager.GetItemIndex(goggles);
            if (index == -1) return;

            var controllerBatterySlots = manager.BatterySlots[index];
            var isDeviceOperable = DeviceOperableSystem.IsDeviceOperable(controllerBatterySlots);
            if (isDeviceOperable) return;

            // Goggles ran out of batteries
            // Ideally the screen would fade black instead of exiting drone... this is okay for now I guess.
            _controlDroneMethod?.Invoke(null, [false]);

            NotificationManagerClass.DisplayWarningNotification(
                $"{goggles.LocalizedName()} ran out of batteries",
                ENotificationDurationType.Long
            );
        }

        private static IEnumerator ExitDroneKeyListener()
        {
            var exitDroneField = AccessTools.Field(
                AccessTools.TypeByName("FPVDroneModClient.Config.FPVBindsConfig"),
                "ExitDrone"
            );
            if (exitDroneField?.GetValue(null) is not ConfigEntry<KeyCode> exitDroneKey)
            {
                LoggerUtil.Error("FPVBindsConfig.ExitDrone could not be found! You're stuck in drone view");
                yield break;
            }

            while (true)
            {
                if (Input.GetKeyDown(exitDroneKey.Value) || Input.GetKeyDown(KeyCode.Escape))
                {
                    _controlDroneMethod?.Invoke(null, [false]);
                    yield break;
                }
                yield return null;
            }
        }
    }
    #endregion

    #region Common Patch Methods
    /// <summary>
    /// Sets the drone's remaining battery based on the battery slot's charge.
    /// Average of batteries in slots.
    /// </summary>
    private static void SetDroneRemainingBattery(object droneControllerObj)
    {
        var sumBatteryValue = 0f;
        var batterySlots = droneControllerObj.GetOrCreateBatterySlots();
        foreach (var batterySlot in batterySlots)
        {
            if (batterySlot.ContainedItem is not { } batteryItem) continue;

            if (batteryItem.TryGetItemComponent(out ResourceComponent resourceComponent))
            {
                sumBatteryValue += resourceComponent.Value;
            }
            else
            {
                LoggerUtil.Warning("Found battery in drone but no resource component!");
            }
        }

        var factor = _maxBattery(droneControllerObj) / 100f;
        _batteryRemaining(droneControllerObj) = (sumBatteryValue / batterySlots.Length) * factor;
#if DEBUG
        LoggerUtil.Info($"Set drone RemainingBattery to {_batteryRemaining(droneControllerObj)}");
#endif
    }

    /// <summary>
    /// Set manager active states for battery draining
    /// </summary>
    private static void SetDroneDevicesActiveState(bool isActive)
    {
        var manager = Singleton<DeviceManager>.Instance;
        if (manager == null) return;

        // Radio controller
        var controller = CanPilotDronePatch.CurrentRadioController;
        if (controller is not null)
        {
            var index = manager.GetItemIndex(controller);
            if (index != -1)
            {
                manager.IsActive[index] = isActive;
            }
        }

        // Goggles
        var goggles = CanPilotDronePatch.CurrentGoggles;
        if (goggles is not null)
        {
            var index = manager.GetItemIndex(goggles);
            if (index != -1)
            {
                manager.IsActive[index] = isActive;
            }
        }
    }
    #endregion
}

public static class ExtraDroneProperties
{
    private static readonly ConditionalWeakTable<Item, Properties> _properties = [];

    [UsedImplicitly]
    public class Properties
    {
        public Slot[] BatterySlot;
    }

    public static bool GetBatterySlot(this Item drone, out Slot[] batterySlot)
    {
        var found = _properties.TryGetValue(drone, out var properties);
        batterySlot = properties?.BatterySlot;
        return found;
    }

    public static void SetBatterySlot(this Item drone, Slot[] batterySlot)
    {
        _properties.GetOrCreateValue(drone).BatterySlot = batterySlot;
    }

    public static Slot[] GetOrCreateBatterySlots(this object droneControllerObj)
    {
        if (droneControllerObj is not MonoBehaviour monoBehaviour)
        {
            throw new ArgumentException($"Object is not MonoBehaviour in {droneControllerObj.GetType().Name}");
        }
        if (monoBehaviour.GetComponent<LootItem>().Item is not CompoundItem droneItem)
        {
            throw new ArgumentException("Drone item cannot be found in baseDroneController");
        }

        if (droneItem.GetBatterySlot(out var batterySlots)) return batterySlots;

        batterySlots = droneItem.GetBatterySlots();
        droneItem.SetBatterySlot(batterySlots);
        return batterySlots;
    }
}
