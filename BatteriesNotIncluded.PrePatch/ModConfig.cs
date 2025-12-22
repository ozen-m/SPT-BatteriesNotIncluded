using Newtonsoft.Json;

namespace BatteriesNotIncluded.PrePatch;

public record ModConfig
{
    [JsonProperty("saveSightsState")]
    public bool SaveSightsState { get; set; } = false;
}
