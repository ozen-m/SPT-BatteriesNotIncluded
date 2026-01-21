using System;
using System.Collections.Generic;
using BatteriesNotIncluded.External;
using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Systems;
using BatteriesNotIncluded.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.SinglePlayer.Utils.InRaid;
using UnityEngine;

namespace BatteriesNotIncluded.Managers;

public class DeviceManager : MonoBehaviour
{
    public readonly List<Item> Devices = [];
    public readonly List<float> DrainPerSecond = [];
    public readonly List<bool> IsOperable = [];
    public readonly List<bool> IsPrevOperable = [];
    public readonly List<bool> IsActive = [];
    public readonly List<bool> IsPrevActive = [];

    public readonly List<Slot[]> BatterySlots = [];
    public readonly List<GClass3379> RelatedComponentRef = [];

    private readonly List<ISystem> _systems = [];
    private readonly List<IManualSystem> _manualSystems = [];

    private readonly List<Action> _unsubscribeEvents = [];
    private readonly List<Item> _playerItemsScratch = new(128);

    /// <summary>
    /// Key: Device item ID, Value: index
    /// </summary>
    private readonly Dictionary<string, int> _indexLookup = [];

    private SightModVisualHandler _sightModVisualHandler;
    private TacticalVisualHandler _tacticalVisualHandler;
    private GameWorld _gameWorld;
    private bool _gameStarted;

    public void Start()
    {
        _sightModVisualHandler = new SightModVisualHandler(this);
        _tacticalVisualHandler = new TacticalVisualHandler(this);

        _systems.Add(new LowBatterySystem(BatteriesNotIncluded.LowBatterySystemInterval.Value * 1000f));

        if (Fika.IsFikaClient)
        {
            return;
        }

        _manualSystems.Add(new DeviceOperableSystem());
        _manualSystems.Add(new DeviceActiveSystem());
        _manualSystems.Add(new DeviceStateEventSystem());
        _manualSystems.Add(new DeviceEnforcementSystem());
        _systems.Add(new BatteryDrainSystem(1000f));
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

    public void ManualUpdate(Item item) => ManualUpdate(item.Id);

    public void ManualUpdate(string itemId)
    {
        var index = GetItemIndex(itemId);
        if (index == -1) return;

        foreach (IManualSystem system in _manualSystems)
        {
            system.Run(this, index);
        }
    }

    public void OnDestroy()
    {
        _gameWorld.OnPersonAdd -= RegisterPlayerItems;
        _gameWorld.AfterGameStarted -= RunAfterGameStart;

        foreach (Action unsubscribe in _unsubscribeEvents)
        {
            unsubscribe();
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
            return UpdateDevice(existingIndex, item, batterySlots);
        }

        int i = Devices.Count;

        _indexLookup[itemId] = i;
        Devices.Add(item);
        DrainPerSecond.Add(deviceData.DrainPerSecond);

        var relatedComponent = GetRelatedComponentToSet(item);
        var isToggled = DeviceActiveSystem.IsDeviceToggled(relatedComponent);

        IsOperable.Add(isToggled);
        IsPrevOperable.Add(isToggled);
        IsActive.Add(isToggled);
        IsPrevActive.Add(isToggled);

        BatterySlots.Add(batterySlots);
        RelatedComponentRef.Add(relatedComponent);

        LoggerUtil.Debug($"Device {item.LocalizedShortName()} ({itemId}) added");

        ManualUpdate(item);

        SubscribeToComponent(relatedComponent);
        if (!Fika.IsFikaClient)
        {
            // Only the server needs to run this
            SubscribeToDeviceSlots(batterySlots);
        }

        return i;
    }

    private int UpdateDevice(int index, CompoundItem item, Slot[] batterySlots)
    {
        Devices[index] = item;
        BatterySlots[index] = batterySlots;

        var relatedComponent = GetRelatedComponentToSet(item);
        RelatedComponentRef[index] = relatedComponent;

        LoggerUtil.Debug($"Device {item.LocalizedShortName()} ({item.Id}) updated");

        // Previous subscription is a leak!
        SubscribeToComponent(relatedComponent);
        if (!Fika.IsFikaClient)
        {
            // Only the server needs to run this
            SubscribeToDeviceSlots(batterySlots);
        }

        return index;
    }

    /// <summary>
    /// Check if an item is operable.
    /// </summary>
    /// <returns>Defaults to <c>true</c> if item is not found</returns>
    public bool GetIsOperable(Item item) => GetIsOperable(item.Id);

    /// <summary>
    /// Check if an item is operable.
    /// </summary>
    /// <returns>Defaults to <c>true</c> if item is not found</returns>
    public bool GetIsOperable(string itemId)
    {
        var index = _indexLookup.GetValueOrDefault(itemId, -1);
        return index == -1 || IsOperable[index];
    }

    public void EnforceSightVisibility(Item item)
    {
        var index = GetItemIndex(item);
        if (index == -1) return;

        var relatedComponent = RelatedComponentRef[index];
        if (relatedComponent is TogglableComponent togglable)
        {
            SetSightVisibility(item, togglable!.On && IsActive[index]);
        }
    }

    public void SetSightVisibility(Item item, bool shouldBeActive) => _sightModVisualHandler.SetSightVisibility(item, shouldBeActive);

    public void UpdateDeviceMode(TacticalComboVisualController controller) => _tacticalVisualHandler.UpdateDeviceMode(controller);

    public void UpdateLightVisibility(Item item) => _tacticalVisualHandler.UpdateLightVisibility(item);

    public bool IsItemRegistered(Item item) => IsItemRegistered(item.Id);

    public bool IsItemRegistered(string itemId) => _indexLookup.ContainsKey(itemId);

    public void Remove(Item item)
    {
        var index = GetItemIndex(item);
        if (index == -1) return;

        // Update index lookup
        var lastElement = Devices[^1];
        _indexLookup[lastElement.Id] = index;
        _indexLookup.Remove(item.Id);

        RemoveAt(index);
    }

    #region FIKA
    public event Action<string, int, Item> OnAddBatteryToSlot;

    public Action SubscribeToOnDeviceStateChanged(Action<string, bool, bool> action)
    {
        Action unsubscribeAction = null;
        foreach (var system in _manualSystems)
        {
            if (system is not DeviceStateEventSystem deviceStateSystem) continue;

            deviceStateSystem.OnDeviceStateChanged += action;
            unsubscribeAction = () => deviceStateSystem.OnDeviceStateChanged -= action;
        }

        return unsubscribeAction;
    }

    public Action SubscribeToOnDrainResource(Action<string, int, float> action)
    {
        Action unsubscribeAction = null;
        foreach (var system in _systems)
        {
            if (system is not BatteryDrainSystem drainBatterySystem) continue;

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
        _tacticalVisualHandler.RemoveDestroyedControllers();
        RegisterWorldLootItems();
    }

    private void RegisterPlayerItems(IPlayer iPlayer)
    {
        if (iPlayer is not Player player) return;

        player.Inventory.Equipment.GetAllItemsNonAlloc(_playerItemsScratch, false, false);
        foreach (var item in _playerItemsScratch)
        {
            TryRegisterItem(item, player, true);
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
            TryRegisterItem(item, null, false);

            // TODO: Add batteries to world items?
        }

        worldItems.Clear();
    }

    /// <summary>
    /// Register a device into the manager, if it is a battery-operated device
    /// </summary>
    /// <param name="isPlayerItem">Used to avoid Unity null checks</param>
    private void TryRegisterItem(Item item, Player player, bool isPlayerItem)
    {
        if (item is not CompoundItem compoundItem) return;
        if (!BatteriesNotIncluded.GetDeviceData(compoundItem.TemplateId, out var deviceData)) return;

        Slot[] batterySlots = compoundItem.GetBatterySlots(deviceData.SlotCount);

        if (isPlayerItem && (player.SearchController is BotSearchControllerClass || RaidChangesUtil.IsScavRaid))
        {
            // AI controlled, player.IsAI not yet available
            AddBatteriesToDevice(player, compoundItem, batterySlots, ref deviceData);
        }

        Add(compoundItem, batterySlots, ref deviceData);
    }

    private void AddBatteriesToDevice(Player player, Item device, Slot[] batterySlots, ref DeviceData deviceData)
    {
        for (var i = 0; i < batterySlots.Length; i++)
        {
            var slot = batterySlots[i];
            var battery = slot.CreateBatteryForSlot(ref deviceData);
            battery.DrainBattery(player);
            if (!slot.AddBatteryToSlot(battery)) continue;

            OnAddBatteryToSlot?.Invoke(device.Id, i, battery);
        }

        device.TurnOnDevice();
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
            relatedComponent = null;
            LoggerUtil.Warning($"Device {item.LocalizedShortName()} ({item.Id}) does not have a related component");
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
                // Light component doesn't have an event we can subscribe to, do it in UpdateBeamsPatch.
                return;
            case NightVisionComponent nightVisionComponent:
                var item = nightVisionComponent.Togglable.Item;
                _unsubscribeEvents.Add(nightVisionComponent.Togglable.OnChanged.Subscribe(() => ManualUpdate(item)));
                return;
            case ThermalVisionComponent thermalVisionComponent:
                item = thermalVisionComponent.Togglable.Item;
                _unsubscribeEvents.Add(thermalVisionComponent.Togglable.OnChanged.Subscribe(() => ManualUpdate(item)));
                return;
            case TogglableComponent togglableComponent:
            {
                item = togglableComponent.Item;
                _unsubscribeEvents.Add(togglableComponent.OnChanged.Subscribe(() => OnToggle(item)));
                return;
            }
            default:
                LoggerUtil.Warning($"Component {component} is not a valid component");
                return;
        }
    }

    /// <summary>
    /// Run manual system for device if battery is added/removed from device slots.
    /// </summary>
    private void SubscribeToDeviceSlots(Slot[] batterySlots)
    {
        foreach (var slot in batterySlots)
        {
            var parentItem = slot.ParentItem;
            _unsubscribeEvents.Add(slot.ReactiveContainedItem.Subscribe((_) => ManualUpdate(parentItem)));
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
                EnforceSightVisibility(item);
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
        LoggerUtil.Debug($"Cannot find item index: {item.LocalizedShortName()} (id: {item.Id}) (template: {item.StringTemplateId})");
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
        DrainPerSecond.SwapRemoveAt(index);
        IsOperable.SwapRemoveAt(index);
        IsPrevOperable.SwapRemoveAt(index);
        IsActive.SwapRemoveAt(index);
        IsPrevActive.SwapRemoveAt(index);

        BatterySlots.SwapRemoveAt(index);
        RelatedComponentRef.SwapRemoveAt(index);
    }
}
