using BatteriesNotIncluded.Utils;
using HarmonyLib;
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
    private static readonly MongoId _dBatteryId = "5672cb304bdc2dc2088b456a";
    private static readonly MongoId _rechargeableBatteryId = "590a358486f77429692b2790";

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
        foreach (var batt in _batteryIds)
        {
            if (!items.TryGetValue(batt, out TemplateItem template))
            {
                throw new ArgumentOutOfRangeException($"Battery {batt} not found in items");
            }

            template.Properties!.MaxResource = 100;
            template.Properties.Resource = 100;
            template.Properties.ItemSound = "food_tin_can";
            if (batt == _dBatteryId)
            {
                template.Properties.Prefab!.Path = "batteries/cr2032.bundle";
            }
            else if (batt == _rechargeableBatteryId)
            {
                template.Properties.Prefab!.Path = "batteries/cr123.bundle";
            }
        }

        var itemsToUseBatteries = items
            .Values
            .Where(i => itemHelper.IsOfBaseclasses(i.Id, _batteryConsumers));
        AddBatterySlots(itemsToUseBatteries);

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
        var batteryType = GetBatteryType(template.Id);
        if (batteryType == MongoId.Empty()) return;

        if (batteryType is null)
        {
            loggerUtil.Warning($"{itemHelper.GetItemName(template.Id)} ({template.Id}) has no defined battery, defaulting to CR2032");
            batteryType = _dBatteryId;
        }

        // TODO: Add battery to item description
        AddBatteryToItemDescription(template.Id, batteryType.Value, 1);

        Slot batterySlot = new Slot()
        {
            Name = "mod_equipment",
            Id = template.Id,
            Parent = template.Id,
            Properties = new SlotProperties()
            {
                // TODO: Multiple battery slots
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

        template.Properties!.Slots = template.Properties.Slots.AddItem(batterySlot);
        Interlocked.Increment(ref counter);
        // loggerUtil.Debug($"{itemHelper.GetItemName(template.Id)} ({template.Id}) added slot with compatible battery {itemHelper.GetItemName(batteryType.Value)}");
    }

    private MongoId? GetBatteryType(MongoId id)
    {
        foreach (var (batteryType, batteryHash) in modConfigContainer.ModConfig.Batteries)
        {
            if (batteryHash.Contains(id))
            {
                return batteryType;
            }
        }
        return null;
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
                    localeData["TURNOFF"] = "TURN OFF";
                    localeData["TURNON"] = "TURN ON";

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
