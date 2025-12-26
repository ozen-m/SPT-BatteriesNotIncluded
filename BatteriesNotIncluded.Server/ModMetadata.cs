using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace BatteriesNotIncluded;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.ozen.batteriesnotincluded";
    public override string Name { get; init; } = "Batteries Not Included";
    public override string Author { get; init; } = "ozen";
    public override List<string> Contributors { get; init; } = ["Jiro", "Birgere"];
    public override Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.9");
    public override List<string> Incompatibilities { get; init; } = [];
    public override Dictionary<string, Range> ModDependencies { get; init; }
    public override string Url { get; init; } = "https://github.com/ozen-m/SPT-BatteriesNotIncluded";
    public override bool? IsBundleMod { get; init; } = true;
    public override string License { get; init; } = "MIT";
}
