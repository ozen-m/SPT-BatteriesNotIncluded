using BatteriesNotIncluded.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace BatteriesNotIncluded.Callbacks;

[Injectable]
public class BatteriesCallbacks(HttpResponseUtil httpResponseUtil, ModConfigContainer modConfigContainer)
{
    public ValueTask<string> GetDeviceDataAsync(string url, EmptyRequestData info, MongoId sessionID)
    {
        var payload = modConfigContainer.ModConfig.DeviceBatteryData
            .Where(d => d.Key != "000000000000000000000000")
            .SelectMany(devices => devices.Value)
            .ToDictionary();
        return new ValueTask<string>(httpResponseUtil.NoBody(payload));
    }

    public ValueTask<string> GetBotBatteriesAsync(string url, EmptyRequestData info, MongoId sessionID)
    {
        return new ValueTask<string>(httpResponseUtil.NoBody(modConfigContainer.ModConfig.BotBatteries));
    }
}
