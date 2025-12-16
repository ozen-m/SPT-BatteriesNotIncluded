using System;
using System.Collections.Generic;
using BatteriesNotIncluded.Components;
using BatteriesNotIncluded.Systems;
using BatteriesNotIncluded.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace BatteriesNotIncluded.Managers;

public class DeviceManager : MonoBehaviour
{
    public readonly List<Item> Devices = [];
    public readonly List<float> DrainMultiplier = [];
    public readonly List<bool> IsOperable = [];
    public readonly List<bool> IsPrevOperable = [];
    public readonly List<bool> IsActive = [];

    public readonly List<Slot[]> BatterySlots = [];
    public readonly List<ResourceComponent[]> ResourceComponentRef = [];
    public readonly List<GClass3379> RelatedComponentRef = [];

    private readonly List<ISystem> _systems = [];
    private readonly List<ISystem> _manualSystems = [];

    /// <summary>
    /// Key: Device item ID, Value: index
    /// </summary>
    private readonly Dictionary<string, int> _indexLookup = [];

    private readonly List<Action> _unsubscribeEvents = [];
    private SightModVisualHandler _sightModVisualHandler;

    private void Start()
    {
        _sightModVisualHandler = new SightModVisualHandler(this);

        _manualSystems.Add(new DeviceOperableSystem());
        _manualSystems.Add(new DeviceBridgeSystem());
        _systems.Add(new DrainBatterySystem(1000));
    }

    private void Update()
    {
        foreach (ISystem system in _systems)
        {
            system.Run(this);
        }
    }

    private void ManualUpdate()
    {
        LoggerUtil.Debug("Running Manual Update");
        foreach (ISystem system in _manualSystems)
        {
            system.Run(this);
        }
    }

    public void ManualUpdate(Item item)
    {
        ManualUpdate(item.Id);
    }

    public void ManualUpdate(string itemId)
    {
        LoggerUtil.Debug($"Running Manual Update for {itemId}");
        var index = GetItemIndex(itemId);
        foreach (ISystem system in _manualSystems)
        {
            system.Run(this, index);
        }
    }

    private void OnDestroy()
    {
        foreach (Action action in _unsubscribeEvents)
        {
            action.Invoke();
        }
        _unsubscribeEvents.Clear();
        _sightModVisualHandler.Cleanup();

        Singleton<DeviceManager>.TryRelease(this);
    }

    public int Add(CompoundItem item, ref BatteryData batteryData, Slot[] batterySlots)
    {
        string itemId = item.Id;
        if (_indexLookup.ContainsKey(itemId))
        {
            throw new ArgumentException($"Item {item.LocalizedShortName()} ({itemId}) already exists!");
        }

        int i = Devices.Count;

        Devices.Add(item);
        _indexLookup[itemId] = i;
        DrainMultiplier.Add(batteryData.DrainMultiplier);
        BatterySlots.Add(batterySlots);
        IsOperable.Add(false);
        IsPrevOperable.Add(false);
        IsActive.Add(false);

        ResourceComponentRef.Add(new ResourceComponent[batterySlots.Length]);
        var relatedComponent = GetRelatedComponentToSet(item);
        RelatedComponentRef.Add(relatedComponent);

        SubscribeToComponent(relatedComponent);
        SubscribeToDeviceSlots(batterySlots);

        LoggerUtil.Debug($"Device {item.LocalizedShortName()} ({itemId}) added");
        return i;
    }

    /// <summary>
    /// Check if an item is operable.
    /// </summary>
    /// <returns>Defaults to <c>true</c> if item is not found</returns>
    public bool GetIsOperable(Item item)
    {
        return GetIsOperable(item.Id);
    }

    /// <summary>
    /// Check if an item is operable.
    /// </summary>
    /// <returns>Defaults to <c>true</c> if item is not found</returns>
    public bool GetIsOperable(string itemId)
    {
        var index = _indexLookup.GetValueOrDefault(itemId, -1);
        return index == -1 || IsOperable[index];
    }

    public void RunAfterGameStart()
    {
        _sightModVisualHandler.RemoveDestroyedControllers();

        // Run twice to set IsPrevOperable correctly in DeviceOperableSystem
        ManualUpdate();
        ManualUpdate();
    }

    public void UpdateSightVisibility(Item item)
    {
        var index = GetItemIndex(item);
        if (index == -1) return;

        _sightModVisualHandler.UpdateSightVisibility(item.Id, IsActive[index]);
    }

    public bool IsItemRegistered(Item item) => IsItemRegistered(item.Id);

    public bool IsItemRegistered(string itemId) => _indexLookup.ContainsKey(itemId);

    public void Remove(Item item)
    {
        // I don't think this will ever get used
        var index = GetItemIndex(item);
        if (index == -1)
        {
            throw new ArgumentException($"Can't find item {item.LocalizedShortName()} ({item.Id}) for removal");
        }
        _indexLookup.Remove(item.Id);
        RemoveAt(index);
    }

    private static GClass3379 GetRelatedComponentToSet(Item item)
    {
        // Order matters since Night/ThermalVision also has a TogglableComponent
        GClass3379 relatedComponent;
        if (item.TryGetItemComponent(out LightComponent lightComponent))
        {
            relatedComponent = lightComponent;
        }
        else if (item.TryGetItemComponent(out NightVisionComponent nightVisionComponent))
        {
            relatedComponent = nightVisionComponent;
        }
        else if (item.TryGetItemComponent(out ThermalVisionComponent thermalVisionComponent))
        {
            relatedComponent = thermalVisionComponent;
        }
        else if (item.TryGetItemComponent(out TogglableComponent togglableComponent))
        {
            relatedComponent = togglableComponent;
        }
        else
        {
            throw new ArgumentException($"Device {item.LocalizedShortName()} ({item.Id}) does not have a related component");
        }
        return relatedComponent;
    }

    /// <summary>
    /// Run manual system for device if toggled.
    /// </summary>
    private void SubscribeToComponent(GClass3379 component)
    {
        switch (component)
        {
            case LightComponent:
                // Light component doesn't have an event we can subscribe to, do it in CanChangeLightStatePatch.
                return;
            case NightVisionComponent nightVisionComponent:
                _unsubscribeEvents.Add(nightVisionComponent.Togglable.OnChanged.Subscribe(() =>
                    ManualUpdate(nightVisionComponent.Togglable.Item)
                ));
                return;
            case ThermalVisionComponent thermalVisionComponent:
                _unsubscribeEvents.Add(thermalVisionComponent.Togglable.OnChanged.Subscribe(() =>
                    ManualUpdate(thermalVisionComponent.Togglable.Item)
                ));
                return;
            case TogglableComponent togglableComponent:
            {
                _unsubscribeEvents.Add(togglableComponent.OnChanged.Subscribe(() =>
                    OnToggle(togglableComponent.Item)
                ));
                return;
            }
            default:
                throw new ArgumentException($"Component {component} is not a valid component");
        }
    }

    /// <summary>
    /// Run manual system for device if battery is added/removed from device slots.
    /// </summary>
    private void SubscribeToDeviceSlots(Slot[] batterySlots)
    {
        foreach (var slot in batterySlots)
        {
            _unsubscribeEvents.Add(slot.ReactiveContainedItem.Subscribe((_) => ManualUpdate(slot.ParentItem)));
        }
    }

    private void OnToggle(Item item)
    {
        ManualUpdate(item);
        if (item.Owner is not Player.PlayerInventoryController playerInvCont) return;

        playerInvCont.Player_0.PlayTacticalSound();
        if (!playerInvCont.Player_0.IsYourPlayer || item.CurrentAddress is GClass3393) return;

        switch (item)
        {
            case HeadphonesItemClass:
                playerInvCont.Player_0.UpdatePhonesReally();
                break;
            case SightsItemClass:
                UpdateSightVisibility(item);
                break;
        }
    }

    private int GetItemIndex(Item item)
    {
#if DEBUG
        if (_indexLookup.TryGetValue(item.Id, out var index))
        {
            return index;
        }
        LoggerUtil.Debug($"Cannot find item index: {item.LocalizedShortName()} ({item.Id})");
        return -1;
#endif

        return GetItemIndex(item.Id);
    }

    private int GetItemIndex(string itemId)
    {
#if DEBUG
        if (_indexLookup.TryGetValue(itemId, out var index))
        {
            return index;
        }
        LoggerUtil.Debug($"Cannot find item index: ({itemId})");
        return -1;
#endif

        return _indexLookup.GetValueOrDefault(itemId, -1);
    }

    private void RemoveAt(int index)
    {
        Devices.SwapRemoveAt(index);
        DrainMultiplier.SwapRemoveAt(index);
        BatterySlots.SwapRemoveAt(index);
        IsOperable.SwapRemoveAt(index);
        IsPrevOperable.SwapRemoveAt(index);
        IsActive.SwapRemoveAt(index);

        ResourceComponentRef.SwapRemoveAt(index);
        RelatedComponentRef.SwapRemoveAt(index);
    }
}
