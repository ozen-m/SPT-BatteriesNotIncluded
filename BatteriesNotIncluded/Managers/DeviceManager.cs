using System;
using System.Collections.Generic;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Models;
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
    public readonly List<GClass3379> RelatedComponentRef = [];

    private readonly List<ISystem> _systems = [];
    private readonly List<ISystem> _manualSystems = [];

    private readonly List<Action> _unsubscribeEvents = [];
    private readonly List<Item> _playerItemsScratch = new(128);

    /// <summary>
    /// Key: Device item ID, Value: index
    /// </summary>
    private readonly Dictionary<string, int> _indexLookup = [];

    private SightModVisualHandler _sightModVisualHandler;
    private GameWorld _gameWorld;
    private bool _gameStarted;

    public void Start()
    {
        _sightModVisualHandler = new SightModVisualHandler(this);

        if (Fika.IsFikaClient)
        {
            return;
        }

        _manualSystems.Add(new DeviceOperableSystem());
        _manualSystems.Add(new DeviceBridgeSystem());
        _systems.Add(new DrainBatterySystem(1000));
    }

    public void SubscribeToGameWorld(GameWorld gameWorld)
    {
        _gameWorld = gameWorld;
        _gameWorld.OnPersonAdd += RegisterPlayerItems;
        _gameWorld.AfterGameStarted += RunAfterGameStart;
    }

    public void Update()
    {
        if (!_gameStarted) return;

        foreach (ISystem system in _systems)
        {
            system.Run(this);
        }
    }

    public void ManualUpdate()
    {
        if (!_gameStarted) return;

        LoggerUtil.Debug("Running Manual Update");
        foreach (ISystem system in _manualSystems)
        {
            system.Run(this);
        }
    }

    public void ManualUpdate(Item item)
    {
        LoggerUtil.Debug($"Running Manual Update for {item.Id}");
        ManualUpdate(item.Id);
    }

    public void ManualUpdate(string itemId)
    {
        if (!_gameStarted) return;

        var index = GetItemIndex(itemId);
        foreach (ISystem system in _manualSystems)
        {
            system.Run(this, index);
        }
    }

    public void OnDestroy()
    {
        _gameWorld.OnPersonAdd -= RegisterPlayerItems;
        _gameWorld.AfterGameStarted -= RunAfterGameStart;

        foreach (Action action in _unsubscribeEvents)
        {
            action.Invoke();
        }
        _unsubscribeEvents.Clear();
        _sightModVisualHandler.Cleanup();

        Singleton<DeviceManager>.TryRelease(this);
    }

    public int Add(CompoundItem item, Slot[] batterySlots, ref DeviceData deviceData)
    {
        string itemId = item.Id;
        if (_indexLookup.TryGetValue(itemId, out var existingIndex))
        {
            // LoggerUtil.Warning($"Item {item.LocalizedShortName()} ({itemId}) already exists, updating");
            return UpdateDevice(existingIndex, item, batterySlots);
        }

        int i = Devices.Count;

        Devices.Add(item);
        _indexLookup[itemId] = i;
        DrainMultiplier.Add(deviceData.DrainRate);
        BatterySlots.Add(batterySlots);
        IsOperable.Add(false);
        IsPrevOperable.Add(false);
        IsActive.Add(false);

        var relatedComponent = GetRelatedComponentToSet(item);
        RelatedComponentRef.Add(relatedComponent);

        LoggerUtil.Debug($"Device {item.LocalizedShortName()} ({itemId}) added");

        SubscribeToComponent(relatedComponent);
        if (!Fika.IsFikaClient)
        {
            // No need to run manual update
            SubscribeToDeviceSlots(batterySlots);
        }

        return i;
    }

    public int UpdateDevice(int index, CompoundItem item, Slot[] batterySlots)
    {
        Devices[index] = item;
        BatterySlots[index] = batterySlots;

        var relatedComponent = GetRelatedComponentToSet(item);
        RelatedComponentRef[index] = relatedComponent;

        LoggerUtil.Debug($"Device {item.LocalizedShortName()} ({item.Id}) updated");

        SubscribeToComponent(relatedComponent);
        if (!Fika.IsFikaClient)
        {
            // No need to run manual update
            SubscribeToDeviceSlots(batterySlots);
        }

        return index;
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

    #region FIKA
    public event Action<string, int, Item> OnAddBatteryToSlot;

    public Action SubscribeToOnSetDeviceOperable(Action<string, bool, bool> action)
    {
        Action unsubscribeAction = null;
        foreach (var system in _manualSystems)
        {
            if (system is not DeviceOperableSystem drainBatterySystem) continue;

            drainBatterySystem.OnSetDeviceOperable += action;
            unsubscribeAction = () => drainBatterySystem.OnSetDeviceOperable -= action;
        }

        return unsubscribeAction;
    }

    public Action SubscribeToOnSetDeviceActive(Action<string, bool> action)
    {
        Action unsubscribeAction = null;
        foreach (var system in _manualSystems)
        {
            if (system is not DeviceBridgeSystem deviceBridgeSystem) continue;

            deviceBridgeSystem.OnSetDeviceActive += action;
            unsubscribeAction = () => deviceBridgeSystem.OnSetDeviceActive -= action;
        }

        return unsubscribeAction;
    }

    public Action SubscribeToOnDrainResource(Action<string, int, float> action)
    {
        Action unsubscribeAction = null;
        foreach (var system in _systems)
        {
            if (system is not DrainBatterySystem drainBatterySystem) continue;

            drainBatterySystem.OnDrainResource += action;
            unsubscribeAction = () => drainBatterySystem.OnDrainResource -= action;
        }

        return unsubscribeAction;
    }
    #endregion

    private void RunAfterGameStart()
    {
        _gameStarted = true;
        _sightModVisualHandler.RemoveDestroyedControllers();
        RegisterWorldLootItems();

        // Run twice to set IsPrevOperable correctly in DeviceOperableSystem
        ManualUpdate();
        ManualUpdate();
    }

    private void RegisterPlayerItems(IPlayer iPlayer)
    {
        if (iPlayer is not Player player) return;

        player.Inventory.Equipment.GetAllItemsNonAlloc(_playerItemsScratch, false, false);
        foreach (var item in _playerItemsScratch)
        {
            RegisterItem(item, true, player);
        }

        _playerItemsScratch.Clear();
    }

    private void RegisterWorldLootItems()
    {
        var worldItems = new List<Item>(3072);
        foreach (var itemOwner in _gameWorld.ItemOwners.Keys)
        {
            // Owner is a player, already registered in `RegisterPlayerItems`
            if (itemOwner is InventoryController) continue;

            itemOwner.RootItem.GetAllItemsNonAlloc(
                worldItems,
                false,
                itemOwner.RootItem is not LootContainerItemClass
            );
        }
        foreach (var item in worldItems)
        {
            RegisterItem(item, false, null);
        }

        worldItems.Clear();
    }

    private void RegisterItem(Item item, bool isPlayerItem, Player player)
    {
        if (item is not CompoundItem compoundItem) return;
        if (!BatteriesNotIncluded.GetDeviceData(compoundItem.TemplateId, out var deviceData)) return;

        var batterySlots = compoundItem.GetBatterySlots(deviceData.SlotCount);
        Add(compoundItem, batterySlots, ref deviceData);

        if (!isPlayerItem || player.SearchController is not BotSearchControllerClass /* not AI controlled, player.IsAI not yet available */) return;

        for (var i = 0; i < batterySlots.Length; i++)
        {
            var slot = batterySlots[i];
            var battery = slot.CreateBatteryForSlot(ref deviceData);
            battery.DrainBattery(player);
            if (!slot.AddBatteryToSlot(battery)) continue;

            OnAddBatteryToSlot?.Invoke(compoundItem.Id, i, battery);
        }

        compoundItem.TurnOnDevice();
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

    public int GetItemIndex(Item item)
    {
#if DEBUG
        if (_indexLookup.TryGetValue(item.Id, out var index))
        {
            return index;
        }
        LoggerUtil.Debug($"Cannot find item index: {item.LocalizedShortName()} ({item.Id})");
        return -1;
#else
        return GetItemIndex(item.Id);
#endif
    }

    public int GetItemIndex(string itemId)
    {
#if DEBUG
        if (_indexLookup.TryGetValue(itemId, out var index))
        {
            return index;
        }
        LoggerUtil.Debug($"Cannot find item index: ({itemId})");
        return -1;
#else
        return _indexLookup.GetValueOrDefault(itemId, -1);
#endif
    }

    private void RemoveAt(int index)
    {
        Devices.SwapRemoveAt(index);
        DrainMultiplier.SwapRemoveAt(index);
        BatterySlots.SwapRemoveAt(index);
        IsOperable.SwapRemoveAt(index);
        IsPrevOperable.SwapRemoveAt(index);
        IsActive.SwapRemoveAt(index);

        RelatedComponentRef.SwapRemoveAt(index);
    }
}
