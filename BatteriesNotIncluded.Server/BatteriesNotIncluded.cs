using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;
using Version = SemanticVersioning.Version;

namespace BatteriesNotIncluded;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration)]
public class BatteriesNotIncluded(
    ConfigUtil configUtil,
    LoggerUtil loggerUtil,
    JsonUtil jsonUtil,
    DatabaseService databaseService,
    ItemHelper itemHelper,
    ServerLocalisationService localeService,
    ApbsCompatibility apbsCompatibility,
    IReadOnlyList<SptMod> sptMods
) : IOnLoad
{
    private Dictionary<string, LazyLoad<Dictionary<string, string>>> _globalLocales;

    public Task OnLoad()
    {
        var localesPath = Path.Combine(configUtil.ConfigPath, "locales");
        LoadLocales(localesPath);

        if (!configUtil.ModConfig.Enabled)
        {
            loggerUtil.Warning(localeService.GetText("load-disabled"));
            return Task.CompletedTask;
        }

        Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        foreach (var batteryId in _batteryIds)
        {
            if (!items.TryGetValue(batteryId, out TemplateItem template))
            {
                throw new ArgumentOutOfRangeException($"Battery {batteryId} not found in items");
            }

            template.Properties!.MaxResource = 100;
            template.Properties.Resource = 100;
            template.Properties.ItemSound = "food_tin_can";
            if (batteryId == _cr2032BatteryId)
            {
                template.Properties.Prefab!.Path = "batteries/cr2032.bundle";
            }
            else if (batteryId == _cr123ABatteryId)
            {
                template.Properties.Prefab!.Path = "batteries/cr123.bundle";
            }
        }

        var deviceTemplates = items
            .Values
            .Where(i => itemHelper.IsOfBaseclasses(i.Id, _batteryConsumers));
        AddBatterySlots(deviceTemplates);

        if (sptMods.FirstOrDefault(m => m.ModMetadata.ModGuid == "com.pein.fpvdronemod") is { } droneMod)
        {
            var kamikazeDrone = new MongoId("690e9265772bfce1d043966d");
            var reconDrone = new MongoId("695f8753d24480b7b1921023");
            var radioController = new MongoId("68f5333abc75f09d96d97500");
            var goggles = new MongoId("68f51bfb3e92385d1908db68");

            var minimumVersion = new Version(0, 5, 0);
            var maximumVersion = new Version(0, 5, 0);
            if (droneMod.ModMetadata.Version >= minimumVersion && droneMod.ModMetadata.Version <= maximumVersion)
            {
                HashSet<MongoId> droneIds = [kamikazeDrone, reconDrone, radioController, goggles];
                var droneTemplates = items
                    .Values
                    .Where(i => droneIds.Contains(i.Id));
                AddBatterySlots(droneTemplates);
            }
            else
            {
                loggerUtil.Warning($"{droneMod.ModMetadata.Name} detected with incompatible version {droneMod.ModMetadata.Version} (Min: {minimumVersion} Max: {maximumVersion})");
            }

            // Has its own drain system
            configUtil.ModConfig.DeviceBatteryDefinitions.Remove(kamikazeDrone);
            configUtil.ModConfig.DeviceBatteryDefinitions.Remove(reconDrone);
        }

        AddBatteryBarterTrades();
        AddBatteriesToModPool();
        AddBatteriesToSicc(items.GetValueOrDefault(ItemTpl.CONTAINER_SICC));
        AddBatteriesToProfileTemplates();
        AddBatteriesToTraderItemsBuy();

        _globalLocales = null;
        loggerUtil.Success(localeService.GetText("load-success"));
        return Task.CompletedTask;
    }

    private void AddBatterySlots(IEnumerable<TemplateItem> itemTemplates)
    {
        var counter = 0;

        itemTemplates
            .AsParallel()
            .ForAll(i => ProcessTemplate(i, ref counter));

        loggerUtil.Info(localeService.GetText("process-total_devices", counter));
    }

    private void ProcessTemplate(TemplateItem template, ref int counter)
    {
        var deviceData = GetDeviceData(template.Id);
        if (deviceData is null)
        {
            deviceData = SetDeviceDefaultBattery(template.Id);
            loggerUtil.Warning(localeService.GetText("process-default_battery", new
            {
                deviceName = itemHelper.GetItemName(template.Id),
                deviceId = template.Id
            }));
        }

        if (deviceData.Battery == MongoId.Empty()) return;

        AddBatteryToItemDescription(template.Id, deviceData);

        var newSlots = new Slot[deviceData.SlotCount];
        for (var i = 0; i < newSlots.Length; i++)
        {
            newSlots[i] = new Slot
            {
                Name = $"mod_equipment_00{i}",
                Id = template.Id,
                Parent = template.Id,
                Properties = new SlotProperties
                {
                    Filters =
                    [
                        new SlotFilter
                        {
                            Shift = 0d,
                            Filter = [deviceData.Battery]
                        }
                    ]
                },
                Required = false,
                MergeSlotWithChildren = false,
                Prototype = "55d30c4c4bdc2db4468b457e"
            };
        }
        template.Properties!.Slots = template.Properties.Slots is not null
            ? template.Properties.Slots.Concat(newSlots)
            : newSlots;

        Interlocked.Increment(ref counter);
        loggerUtil.Debug(localeService.GetText("process-add_battery_slot", new
        {
            deviceName = itemHelper.GetItemName(template.Id),
            deviceId = template.Id,
            slotCount = deviceData.SlotCount,
            batteryName = itemHelper.GetItemName(deviceData.Battery),
            runtime = (int)(deviceData.GameRuntimeSecs / 60d)
        }));
    }

    private DeviceData GetDeviceData(MongoId deviceId) =>
        configUtil.ModConfig.DeviceBatteryDefinitions.GetValueOrDefault(deviceId, null);

    private DeviceData SetDeviceDefaultBattery(MongoId deviceId)
    {
        var defaultData = DeviceData.Default;
        configUtil.ModConfig.DeviceBatteryDefinitions[deviceId] = defaultData;
        return defaultData;
    }

    private void AddBatteryToItemDescription(MongoId deviceId, DeviceData deviceData)
    {
        string runtime = string.Empty;
        var isTacticalDevice = itemHelper.IsOfBaseclasses(deviceId, _tacticalDevices);
        if (!isTacticalDevice)
        {
            var t = TimeSpan.FromSeconds(deviceData.GameRuntimeSecs);
            var hours = (int)t.TotalHours;
            var minutes = t.Minutes;
            runtime = hours > 0 ? $"{hours}h {minutes:D2}m" : $"{minutes:D2}m";
        }

        foreach (var (_, lazyLoadLocale) in _globalLocales)
        {
            lazyLoadLocale.AddTransformer((localeData) =>
            {
                var slotsLocalized = ReplacePlaceholder(localeData["description-slots"], deviceData.SlotCount);
                var runtimeLocalized = !isTacticalDevice
                    ? ReplacePlaceholder(localeData["description-runtime"], runtime)
                    : localeData["description-runtime_tactical"];

                localeData[$"{deviceId} Description"] = $"{slotsLocalized} {localeData[$"{deviceData.Battery} Name"]}\n{runtimeLocalized}\n\n{localeData[$"{deviceId} Description"]}";
                return localeData;
            });
        }
    }

    /// <summary>
    /// This suppresses warnings in the SPT server console/logs.
    /// We still add them, if not spawned since they're rolled, via client.
    /// </summary>
    private void AddBatteriesToModPool()
    {
        var botTypes = databaseService.GetBots().Types;

        // APBS
        const int apbsTierCount = 8;
        var apbsMods = apbsCompatibility.GetModsData(apbsTierCount);

        foreach (var (deviceId, deviceData) in configUtil.ModConfig.DeviceBatteryDefinitions)
        {
            if (deviceData.Battery == MongoId.Empty()) continue;

            foreach (var (_, botType) in botTypes)
            {
                var mods = botType?.BotInventory.Mods;
                if (mods is null) continue;

                ProcessModPoolForDevice(mods, deviceId, deviceData);
            }

            // APBS
            foreach (var mods in apbsMods ?? [])
            {
                if (mods is null) continue;

                ProcessModPoolForDevice(mods, deviceId, deviceData);
            }
        }

        loggerUtil.Debug(localeService.GetText("process-mod_pools"));
    }

    private static void ProcessModPoolForDevice(Dictionary<MongoId, Dictionary<string, HashSet<MongoId>>> mods, MongoId itemId, DeviceData data)
    {
        if (mods.TryGetValue(itemId, out var botMods))
        {
            for (var i = 0; i < data.SlotCount; i++)
            {
                if (botMods.TryGetValue($"mod_equipment_00{i}", out var botMod))
                {
                    botMod.Add(data.Battery);
                    continue;
                }
                botMods[$"mod_equipment_00{i}"] = [data.Battery];
            }
        }
        else
        {
            var newBotMod = mods[itemId] = [];
            HashSet<MongoId> botMod = [data.Battery];
            for (var i = 0; i < data.SlotCount; i++)
            {
                newBotMod[$"mod_equipment_00{i}"] = botMod;
            }
        }
    }

    private void AddBatteryBarterTrades()
    {
        var jaeger = databaseService.GetTrader(Traders.JAEGER)!;

        // CR2032 Trade
        var cr2032TradeId = new MongoId("694e8f4d475bbd094c3533ed");
        jaeger.Assort.Items.Add(new Item
        {
            Id = cr2032TradeId,
            Template = _cr2032BatteryId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9999999d
            }
        });
        List<List<BarterScheme>> cr2032Barter =
        [
            [
                new BarterScheme
                {
                    Count = 3d,
                    Template = _cr2032BatteryId
                }
            ]
        ];
        jaeger.Assort.BarterScheme[cr2032TradeId] = cr2032Barter;
        jaeger.Assort.LoyalLevelItems[cr2032TradeId] = 1;

        // CR123A Trade
        var cr123ATradeId = new MongoId("694e91d1ebb9883e123533ee");
        jaeger.Assort.Items.Add(new Item
        {
            Id = cr123ATradeId,
            Template = _cr123ABatteryId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9999999d
            }
        });
        List<List<BarterScheme>> cr123ABarter =
        [
            [
                new BarterScheme
                {
                    Count = 4d,
                    Template = _cr123ABatteryId
                }
            ]
        ];
        jaeger.Assort.BarterScheme[cr123ATradeId] = cr123ABarter;
        jaeger.Assort.LoyalLevelItems[cr123ATradeId] = 1;

        // AA Trade
        var aaTradeId = new MongoId("694e91d46640bf29153533ef");
        jaeger.Assort.Items.Add(new Item
        {
            Id = aaTradeId,
            Template = _aaBatteryId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9999999d
            }
        });
        List<List<BarterScheme>> aaBarter =
        [
            [
                new BarterScheme
                {
                    Count = 3d,
                    Template = _aaBatteryId
                }
            ]
        ];
        jaeger.Assort.BarterScheme[aaTradeId] = aaBarter;
        jaeger.Assort.LoyalLevelItems[aaTradeId] = 1;

        loggerUtil.Debug(localeService.GetText("process-barter_trades"));
    }

    private void AddBatteriesToSicc(TemplateItem item)
    {
        if (!configUtil.ModConfig.SiccContainerBatteries) return;
        if (item.Id != ItemTpl.CONTAINER_SICC) return;

        item.Properties?.Grids?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter?.UnionWith(_batteryIds);

        loggerUtil.Debug(localeService.GetText("process-sicc_container"));
    }

    /// <summary>
    /// Adds batteries to devices for new profiles
    /// </summary>
    private void AddBatteriesToProfileTemplates()
    {
        var profileTemplates = databaseService.GetProfileTemplates();

        foreach (var (_, sides) in profileTemplates)
        {
            if (sides.Usec is not null)
            {
                ProcessTemplateSide(sides.Usec);
            }

            if (sides.Bear is not null)
            {
                ProcessTemplateSide(sides.Bear);
            }
        }
        loggerUtil.Debug(localeService.GetText("process-profile_templates"));
    }

    private void ProcessTemplateSide(TemplateSide side)
    {
        var sideItems = side.Character?.Inventory?.Items;
        if (sideItems is null) return;

        for (var i = 0; i < sideItems.Count; i++)
        {
            var item = sideItems[i];
            var deviceData = GetDeviceData(item.Template);
            if (deviceData is null) continue;
            if (deviceData.Battery == MongoId.Empty()) continue;

            for (var j = 0; j < deviceData.SlotCount; j++)
            {
                var battery = new Item
                {
                    Id = new MongoId(),
                    ParentId = item.Id,
                    Template = deviceData.Battery,
                    SlotId = $"mod_equipment_00{j}"
                };
                sideItems.Add(battery);
            }
        }
    }

    /// <summary>
    /// Adds batteries to all trader's buy list to make it easier to sell devices to a trader
    /// </summary>
    private void AddBatteriesToTraderItemsBuy()
    {
        var traders = databaseService.GetTraders();
        foreach (var (_, trader) in traders)
        {
            var buyIdList = trader.Base.ItemsBuy?.IdList;
            buyIdList?.UnionWith(_batteryIds);
        }
    }

    private void LoadLocales(string localesPath)
    {
        Dictionary<string, Dictionary<string, string>> locales = [];
        if (!Directory.Exists(localesPath))
        {
            throw new FileNotFoundException($"Missing locales directory: {localesPath}");
        }
        try
        {
            var localeFiles = Directory.GetFiles(localesPath, "*.json");
            foreach (var localeFile in localeFiles)
            {
                var language = Path.GetFileNameWithoutExtension(localeFile);
                locales[language] = jsonUtil.DeserializeFromFile<Dictionary<string, string>>(localeFile);
            }
        }
        catch (Exception ex)
        {
            loggerUtil.Error(ex.ToString());
            loggerUtil.Error("Exception while trying to load locales");
        }
        if (locales.Count < 1)
        {
            loggerUtil.Error($"No locale files found under: {localesPath}");
            return;
        }

        _globalLocales = databaseService.GetLocales().Global;
        foreach (var (lang, lazyLoadLocale) in _globalLocales)
        {
            if (locales.TryGetValue(lang, out Dictionary<string, string> locale))
            {
                lazyLoadLocale.AddTransformer((localeData) =>
                {
                    foreach (var (key, value) in locale)
                    {
                        localeData[key] = value;
                    }

                    return localeData;
                });
            }
            else
            {
                // We don't have a locale file for current language, use english
                lazyLoadLocale.AddTransformer((localeData) =>
                {
                    foreach (var (key, value) in locales["en"])
                    {
                        localeData[key] = value;
                    }

                    return localeData;
                });
            }
        }
    }

    /// <summary>
    ///  SPT Code: <see cref="ServerLocalisationService.GetLocalised"/>
    /// </summary>
    private static string ReplacePlaceholder<T>(string text, T value)
        where T : IConvertible
    {
        return text.Replace("%s", value?.ToString() ?? string.Empty);
    }

    private static readonly MongoId _aaBatteryId = ItemTpl.BARTER_AA_BATTERY;
    private static readonly MongoId _cr2032BatteryId = ItemTpl.BARTER_D_SIZE_BATTERY;
    private static readonly MongoId _cr123ABatteryId = ItemTpl.BARTER_RECHARGEABLE_BATTERY;

    private static readonly MongoId[] _batteryIds =
    [
        _aaBatteryId,
        _cr2032BatteryId,
        _cr123ABatteryId
    ];

    private static readonly MongoId[] _batteryConsumers =
    [
        BaseClasses.NIGHT_VISION, // Headwear
        BaseClasses.THERMAL_VISION, // Headwear
        BaseClasses.SPECIAL_SCOPE, // Night/Thermal Sight
        BaseClasses.COLLIMATOR, // Sight
        BaseClasses.COMPACT_COLLIMATOR, // Sight
        // BaseClasses.OPTIC_SCOPE, // Sight
        BaseClasses.HEADPHONES, // Earpiece
        BaseClasses.FLASHLIGHT, // Tactical Device
        BaseClasses.LIGHT_LASER, // Tactical Device
        BaseClasses.TACTICAL_COMBO // Tactical Device
    ];

    private static readonly MongoId[] _tacticalDevices =
    [
        BaseClasses.FLASHLIGHT,
        BaseClasses.TACTICAL_COMBO,
        BaseClasses.LIGHT_LASER
    ];
}
