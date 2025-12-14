using System;
using System.Collections;
using System.Collections.Generic;
using BatteriesNotIncluded.Components;
using BatteriesNotIncluded.Patches;
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
    public readonly List<bool> IsActive = [];

    public readonly List<Slot> BatterySlot = [];
    public readonly List<ResourceComponent> ResourceComponentRef = [];
    public readonly List<GClass3379> RelatedComponentRef = [];

    private readonly List<ISystem> _systems = [];
    private readonly List<ISystem> _manualSystems = [];

    /// <summary>
    /// Key: Device item ID, Value: index
    /// </summary>
    private readonly Dictionary<string, int> _indexLookup = [];

    private readonly List<Action> _unsubscribeEvents = [];

    private SightModVisualHandler _sightModVisualHandler;
    private bool _shouldRunManualUpdate;

    private void Start()
    {
        _sightModVisualHandler = new SightModVisualHandler(this);

        _manualSystems.Add(new DeviceOperableSystem());
        _manualSystems.Add(new DeviceBridgeSystem());
        _systems.Add(new DrainBatterySystem(1000));

        OnItemAddedOrRemovedPatch.OnItemAddedOrRemoved += OnItemAddedOrRemoved;
    }

    private void Update()
    {
        if (_shouldRunManualUpdate)
        {
            _shouldRunManualUpdate = false;
            ManualUpdate();
        }

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
        OnItemAddedOrRemovedPatch.OnItemAddedOrRemoved -= OnItemAddedOrRemoved;
        foreach (Action action in _unsubscribeEvents)
        {
            action.Invoke();
        }
        _unsubscribeEvents.Clear();
        _sightModVisualHandler.Cleanup();

        Singleton<DeviceManager>.TryRelease(this);
    }

    public int Add(CompoundItem item, ref BatteryData batteryData, Slot batterySlot)
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
        BatterySlot.Add(batterySlot);
        IsOperable.Add(false);
        IsActive.Add(false);

        ResourceComponentRef.Add(null);
        var relatedComponent = GetRelatedComponentToSet(item);
        RelatedComponentRef.Add(relatedComponent);

        SubscribeToComponent(relatedComponent);

        LoggerUtil.Debug($"Device {item.LocalizedShortName()} ({itemId}) added");
        return i;
    }

    public void OnItemAddedOrRemoved(Item item)
    {
        if (!item.IsBattery()) return;

        RunManualUpdateNextFrame();
    }

    public void RunManualUpdateNextFrame() => _shouldRunManualUpdate = true;

    /// <summary>
    /// Check if an item is operable.
    /// </summary>
    /// <returns>Defaults to <c>true</c> if item is not found</returns>
    public bool GetIsOperable(Item item)
    {
        var index = GetItemIndex(item);
        return index == -1 || IsOperable[index];
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

    public bool GetIsActive(string itemId)
    {
        var index = _indexLookup.GetValueOrDefault(itemId, -1);
        return index == -1 || IsOperable[index];
    }

    public void RunAfterGameStart()
    {
        _sightModVisualHandler.RemoveDestroyedControllers();
        RunManualUpdateNextFrame();
    }

    public void UpdateSightVisibility(string itemId, int index) =>
        _sightModVisualHandler.UpdateSightVisibility(itemId, IsActive[index]);

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

    private void SubscribeToComponent(GClass3379 component)
    {
        switch (component)
        {
            case LightComponent:
                // Light component doesn't have an event we can subscribe to, do it in CanChangeLightStatePatch.
                return;
            case NightVisionComponent nightVisionComponent:
                _unsubscribeEvents.Add(nightVisionComponent.Togglable.OnChanged.Subscribe(() =>
                    StartCoroutine(RunManualUpdateNextFrame(nightVisionComponent.Togglable.Item))
                ));
                return;
            case ThermalVisionComponent thermalVisionComponent:
                _unsubscribeEvents.Add(thermalVisionComponent.Togglable.OnChanged.Subscribe(() =>
                    StartCoroutine(RunManualUpdateNextFrame(thermalVisionComponent.Togglable.Item))
                ));
                return;
            case TogglableComponent togglableComponent:
            {
                _unsubscribeEvents.Add(togglableComponent.OnChanged.Subscribe(() =>
                    OnToggle(togglableComponent.Item)));
                return;
            }
            default:
                throw new ArgumentException($"Component {component} is not a valid component");
        }
    }

    private void OnToggle(Item item)
    {
        ManualUpdate(item);
        if (item.Owner is Player.PlayerInventoryController playerInvCont)
        {
            playerInvCont.Player_0.PlayTacticalSound();
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

    private IEnumerator RunManualUpdateNextFrame(Item item)
    {
        yield return null; // wait one frame

        ManualUpdate(item);
    }

    private void RemoveAt(int index)
    {
        Devices.SwapRemoveAt(index);
        DrainMultiplier.SwapRemoveAt(index);
        BatterySlot.SwapRemoveAt(index);
        IsOperable.SwapRemoveAt(index);
        IsActive.SwapRemoveAt(index);

        ResourceComponentRef.SwapRemoveAt(index);
        RelatedComponentRef.SwapRemoveAt(index);
    }

    // TODO: Fix helmet light devices!
}
