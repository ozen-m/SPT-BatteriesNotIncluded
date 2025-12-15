using BatteriesNotIncluded.Models;
using BatteriesNotIncluded.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

namespace BatteriesNotIncluded;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)] // TODO: Determine LoadOrder
public class BatteriesNotIncluded(
    ModConfigContainer modConfigContainer,
    LoggerUtil loggerUtil,
    JsonUtil jsonUtil,
    DatabaseService databaseService,
    ItemHelper itemHelper
) : IOnLoad
{
    private static readonly MongoId _aaBatteryId = "5672cb124bdc2d1a0f8b4568";
    private static readonly MongoId _dBatteryId = "5672cb304bdc2dc2088b456a"; // CR2032
    private static readonly MongoId _rechargeableBatteryId = "590a358486f77429692b2790"; // CR123A

    private static readonly MongoId[] _batteryIds = [_aaBatteryId, _dBatteryId, _rechargeableBatteryId];

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

        // TODO: Add file check for PrePatch

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
            if (batteryId == _dBatteryId)
            {
                template.Properties.Prefab!.Path = "batteries/cr2032.bundle";
            }
            else if (batteryId == _rechargeableBatteryId)
            {
                template.Properties.Prefab!.Path = "batteries/cr123.bundle";
            }
        }

        var itemsToUseBatteries = items
            .Values
            .Where(i => itemHelper.IsOfBaseclasses(i.Id, _batteryConsumers));
        AddBatterySlots(itemsToUseBatteries);

        AddToModPool();

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
        var batteryData = GetBatteryData(template.Id);
        var batteryType = batteryData.Battery;
        if (batteryType == MongoId.Empty()) return;

        if (batteryType is null)
        {
            loggerUtil.Warning($"{itemHelper.GetItemName(template.Id)} ({template.Id}) has no defined battery, defaulting to CR2032");
            batteryType = _dBatteryId;
        }

        AddBatteryToItemDescription(template.Id, batteryType.Value, batteryData.Slots);

        Slot[] newSlots = new Slot[batteryData.Slots];
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
                            Filter = [batteryType.Value],
                            // TODO: Multiple battery filters
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
        // loggerUtil.Debug($"{itemHelper.GetItemName(template.Id)} ({template.Id}) added slot with compatible battery {itemHelper.GetItemName(batteryType.Value)}");
    }

    private DeviceData GetBatteryData(MongoId id)
    {
        if (modConfigContainer.ModConfig.NoBattery.TryGetValue(id, out DeviceData deviceData))
        {
            deviceData.Battery = MongoId.Empty();
            return deviceData;
        }
        if (modConfigContainer.ModConfig.CR123A.TryGetValue(id, out deviceData))
        {
            deviceData.Battery = _rechargeableBatteryId;
            return deviceData;
        }
        if (modConfigContainer.ModConfig.CR2032.TryGetValue(id, out deviceData))
        {
            deviceData.Battery = _dBatteryId;
            return deviceData;
        }
        if (modConfigContainer.ModConfig.AA.TryGetValue(id, out deviceData))
        {
            deviceData.Battery = _aaBatteryId;
            return deviceData;
        }
        return DeviceData.Empty;
    }

    private void AddBatteryToItemDescription(MongoId deviceId, MongoId batteryId, int slots)
    {
        foreach (var (_, lazyLoadLocale) in _globalLocales)
        {
            lazyLoadLocale.AddTransformer((localeData) =>
            {
                localeData[$"{deviceId} Description"] = $"Uses {slots}x {localeData[$"{batteryId} Name"]}\n\n{localeData[$"{deviceId} Description"]}";

                return localeData;
            });
        }
    }

    /// <summary>
    /// This suppresses warnings in the SPT server console/logs.
    /// We still add them, if not spawned since they're rolled, via client.
    /// </summary>
    private void AddToModPool()
    {
        foreach ((MongoId deviceId, DeviceData deviceData) in modConfigContainer.ModConfig.CR2032)
        {
            AddBatteriesToModPool(deviceId, _dBatteryId, deviceData.Slots);
        }
        foreach ((MongoId deviceId, DeviceData deviceData) in modConfigContainer.ModConfig.CR123A)
        {
            AddBatteriesToModPool(deviceId, _rechargeableBatteryId, deviceData.Slots);
        }
        foreach ((MongoId deviceId, DeviceData deviceData) in modConfigContainer.ModConfig.AA)
        {
            AddBatteriesToModPool(deviceId, _aaBatteryId, deviceData.Slots);
        }
        loggerUtil.Info($"Added batteries to mod pools");
    }

    private void AddBatteriesToModPool(MongoId itemId, MongoId batteryId, int slots)
    {
        var botTypes = databaseService.GetBots().Types;
        foreach (var (_, value) in botTypes)
        {
            if (value!.BotInventory.Mods.TryGetValue(itemId, out var botMods))
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
                var newBotMod = value.BotInventory.Mods[itemId] = [];
                HashSet<MongoId> botMod = [batteryId];
                for (var i = 0; i < slots; i++)
                {
                    newBotMod[$"mod_equipment_00{i}"] = botMod;
                }
            }
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
                    var en = locales["en"];
                    foreach (var (key, value) in en)
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

    private readonly MongoId[] _batteryConsumers =
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
