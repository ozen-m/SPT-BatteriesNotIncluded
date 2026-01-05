using BatteriesNotIncluded.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace BatteriesNotIncluded.Callbacks;

[Injectable]
public class BatteriesCallbacks(HttpResponseUtil httpResponseUtil, ConfigUtil configUtil)
{
    public ValueTask<string> GetConfigAsync(string url, EmptyRequestData info, MongoId sessionID)
    {
        if (!configUtil.ModConfig.Enabled)
        {
            return new ValueTask<string>(httpResponseUtil.NullResponse());
        }

        var payload = new
        {
            deviceBatteryDefinitions = configUtil.ModConfig.DeviceBatteryDefinitions
                .Where(d => d.Value.Battery != MongoId.Empty())
                .ToDictionary(),
            botBatteries = configUtil.ModConfig.BotBatteries,
            tacticalDevicesDrain = configUtil.ModConfig.TacticalDevicesDrain,
            tacticalDevicesModeOverride =  configUtil.ModConfig.TacticalDevicesModeOverride
        };
        return new ValueTask<string>(httpResponseUtil.NoBody(payload));
    }
}
