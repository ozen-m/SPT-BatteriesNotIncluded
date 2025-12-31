using BatteriesNotIncluded.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace BatteriesNotIncluded.Callbacks;

[Injectable]
public class BatteriesCallbacks(HttpResponseUtil httpResponseUtil, ModConfigContainer modConfigContainer)
{
    public ValueTask<string> GetConfigAsync(string url, EmptyRequestData info, MongoId sessionID)
    {
        if (!modConfigContainer.ModConfig.Enabled)
        {
            return new ValueTask<string>(httpResponseUtil.NullResponse());
        }

        var payload = new
        {
            DeviceBatteryData = modConfigContainer.ModConfig.DeviceBatteryData
                .Where(d => d.Key != MongoId.Empty())
                .SelectMany(devices => devices.Value)
                .ToDictionary(),
            botBatteries = modConfigContainer.ModConfig.BotBatteries,
            tacticalDevicesDrain = modConfigContainer.ModConfig.TacticalDevicesDrain,
            tacticalDevicesModeOverride =  modConfigContainer.ModConfig.TacticalDevicesModeOverride
        };
        return new ValueTask<string>(httpResponseUtil.NoBody(payload));
    }
}
