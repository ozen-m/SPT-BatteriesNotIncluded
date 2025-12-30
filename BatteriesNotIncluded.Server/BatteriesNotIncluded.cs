using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

namespace BatteriesNotIncluded;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration)]
public class BatteriesNotIncluded(
    ModConfigContainer modConfigContainer,
    LoggerUtil loggerUtil,
    JsonUtil jsonUtil,
    DatabaseService databaseService,
    ItemHelper itemHelper
) : IOnLoad
{
    private Dictionary<string, LazyLoad<Dictionary<string, string>>> _globalLocales;

    public Task OnLoad()
    {
        if (!modConfigContainer.ModConfig.Enabled)
        {
            loggerUtil.Warning("is disabled");
            return Task.CompletedTask;
        }

        var localesPath = Path.Combine(modConfigContainer.ConfigPath, "locales");
        LoadLocales(localesPath);

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

        ConvertTacticalDevicesDrain();
        AddToModPool();
        AddJaegerTrades();
        AddBatteriesToSicc(items.GetValueOrDefault(ItemTpl.CONTAINER_SICC));

        loggerUtil.Success("loaded successfully!");
        return Task.CompletedTask;
    }

    private void AddBatterySlots(IEnumerable<TemplateItem> itemTemplates)
    {
        var counter = 0;

        itemTemplates
            .AsParallel()
            .ForAll(i => ProcessTemplate(i, ref counter));

        _globalLocales = null;
        loggerUtil.Info($"Added battery slots to {counter} items");
    }

    private void ProcessTemplate(TemplateItem template, ref int counter)
    {
        var deviceData = GetDeviceData(template.Id);
        var batteryType = deviceData.Battery;
        if (batteryType == MongoId.Empty()) return;

        AddBatteryToItemDescription(template.Id, batteryType, deviceData.SlotCount, deviceData.GameRuntimeSecs);

        var newSlots = new Slot[deviceData.SlotCount];
        for (var i = 0; i < newSlots.Length; i++)
        {
            newSlots[i] = new Slot()
            {
                Name = $"mod_equipment_00{i}",
                Id = template.Id,
                Parent = template.Id,
                Properties = new SlotProperties()
                {
                    Filters =
                    [
                        new SlotFilter()
                        {
                            Shift = 0d,
                            Filter = [batteryType]
                            // TODO: Multiple battery filters?
                        }
                    ]
                },
                Required = false,
                MergeSlotWithChildren = false,
                Prototype = "55d30c4c4bdc2db4468b457e"
            };
        }
        template.Properties!.Slots = template.Properties.Slots != null
            ? template.Properties.Slots.Concat(newSlots)
            : newSlots;

        Interlocked.Increment(ref counter);
        loggerUtil.Debug($"{itemHelper.GetItemName(template.Id)} ({template.Id}) added slot with compatible battery {itemHelper.GetItemName(batteryType)} ({(int)(deviceData.GameRuntimeSecs / 60d)}m)");
    }

    private DeviceData GetDeviceData(MongoId deviceId)
    {
        foreach (var (batteryType, deviceDatas) in modConfigContainer.ModConfig.DeviceBatteryData)
        {
            if (!deviceDatas.TryGetValue(deviceId, out var deviceData)) continue;

            const double maxResourceValue = 100f; // Ideally should come from the battery template
            var gameRuntimeSecs = RuntimeToSeconds(deviceData.RealRuntimeHr) / modConfigContainer.ModConfig.GlobalDrainMult;
            deviceData.Battery = batteryType;
            deviceData.GameRuntimeSecs = gameRuntimeSecs;
            deviceData.DrainPerSecond = maxResourceValue / gameRuntimeSecs;

            return deviceData;
        }

        var defaultData = DeviceData.Default;
        modConfigContainer.ModConfig.DeviceBatteryData[_cr2032BatteryId].Add(deviceId, defaultData);
        loggerUtil.Warning($"{itemHelper.GetItemName(deviceId)} ({deviceId}) has no defined battery, defaulting to CR2032");
        return defaultData;
    }

    private void AddBatteryToItemDescription(MongoId deviceId, MongoId batteryId, int slots, double runtimeSeconds)
    {
        var t = TimeSpan.FromSeconds(runtimeSeconds);
        var hours = (int)t.TotalHours;
        var minutes = t.Minutes;
        var runtime = hours > 0 ? $"{hours}h {minutes:D2}m" : $"{minutes:D2}m";

        foreach (var (_, lazyLoadLocale) in _globalLocales)
        {
            lazyLoadLocale.AddTransformer((localeData) =>
            {
                localeData[$"{deviceId} Description"] = $"Uses {slots}x {localeData[$"{batteryId} Name"]}\nHas a runtime of {runtime}\n\n{localeData[$"{deviceId} Description"]}";

                return localeData;
            });
        }
    }

    private void ConvertTacticalDevicesDrain()
    {
        foreach (var (mode, hours) in modConfigContainer.ModConfig.TacticalDevicesDrain)
        {
            var seconds = RuntimeToSeconds(hours);
            modConfigContainer.ModConfig.TacticalDevicesDrain[mode] = 100d / seconds;
        }
    }

    /// <summary>
    /// This suppresses warnings in the SPT server console/logs.
    /// We still add them, if not spawned since they're rolled, via client.
    /// </summary>
    private void AddToModPool()
    {
        foreach (var (batteryId, deviceDatas) in modConfigContainer.ModConfig.DeviceBatteryData)
        {
            if (batteryId == MongoId.Empty()) continue;

            foreach (var (deviceId, deviceData) in deviceDatas)
            {
                AddBatteriesToModPool(deviceId, batteryId, deviceData.SlotCount);
            }
        }
        loggerUtil.Info($"Added batteries to mod pools");
    }

    private void AddBatteriesToModPool(MongoId itemId, MongoId batteryId, int slots)
    {
        var botTypes = databaseService.GetBots().Types;
        foreach (var (_, botType) in botTypes)
        {
            if (botType!.BotInventory.Mods.TryGetValue(itemId, out var botMods))
            {
                for (var i = 0; i < slots; i++)
                {
                    if (botMods.TryGetValue($"mod_equipment_00{i}", out var botMod))
                    {
                        botMod.Add(batteryId);
                        continue;
                    }
                    botMods[$"mod_equipment_00{i}"] = [batteryId];
                }
            }
            else
            {
                var newBotMod = botType.BotInventory.Mods[itemId] = [];
                HashSet<MongoId> botMod = [batteryId];
                for (var i = 0; i < slots; i++)
                {
                    newBotMod[$"mod_equipment_00{i}"] = botMod;
                }
            }
        }
    }

    private void AddJaegerTrades()
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
                StackObjectsCount = 9999999d,
                BuyRestrictionMax = 4,
                BuyRestrictionCurrent = 0
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
                StackObjectsCount = 9999999d,
                BuyRestrictionMax = 4,
                BuyRestrictionCurrent = 0
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
                StackObjectsCount = 9999999d,
                BuyRestrictionMax = 4,
                BuyRestrictionCurrent = 0
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
    }

    private void AddBatteriesToSicc(TemplateItem item)
    {
        if (!modConfigContainer.ModConfig.SiccContainerBatteries) return;
        if (item.Id != ItemTpl.CONTAINER_SICC) return;

        item.Properties?.Grids?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.Filter?.UnionWith(_batteryIds);
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
                    localeData["TURNOFF"] = "TURN OFF"; // FLIP UP
                    localeData["TURNON"] = "TURN ON"; // FLIP DOWN

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
                    localeData["TURNOFF"] = "TURN OFF"; // FLIP UP
                    localeData["TURNON"] = "TURN ON"; // FLIP DOWN

                    return localeData;
                });
            }
        }
    }

    private static readonly double _logMin = Math.Log10(1d);
    private static readonly double _logRange = Math.Log10(100_000d) - _logMin;

    /// <summary>
    /// Normalized log interpolation.
    /// Map minimum: 1 hour runtime to <see cref="ModConfig.MinGameRuntime"/>;
    ///     maximum: 100,000 hours runtime to <see cref="ModConfig.MaxGameRuntime"/>.
    ///     maximum: 100,000 hours runtime to <see cref="ModConfig.MaxGameRuntime"/>.
    /// </summary>
    /// <param name="runtimeHours">Device battery life in hours</param>
    /// <returns>Real runtime hours mapped to game seconds</returns>
    private double RuntimeToSeconds(double runtimeHours)
    {
        runtimeHours = Math.Clamp(runtimeHours, 1d, 100_000d);
        double tMin = modConfigContainer.ModConfig.MinGameRuntime;
        double tMax = modConfigContainer.ModConfig.MaxGameRuntime;

        double factor = (Math.Log10(runtimeHours) - _logMin) / _logRange;
        return (int)(tMin + (tMax - tMin) * factor);
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
}
