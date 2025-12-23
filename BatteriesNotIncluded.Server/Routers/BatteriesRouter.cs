using BatteriesNotIncluded.Callbacks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace BatteriesNotIncluded.Routers;

[Injectable]
public class BatteriesRouter(BatteriesCallbacks batteriesCallbacks, JsonUtil jsonUtil)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/BatteriesNotIncluded/GetDeviceData",
                async (url, info, sessionId, output) => await batteriesCallbacks.GetDeviceDataAsync(url, info, sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/BatteriesNotIncluded/GetBotBatteries",
                async (url, info, sessionId, output) => await batteriesCallbacks.GetBotBatteriesAsync(url, info, sessionId)
            )
        ])
{
}
