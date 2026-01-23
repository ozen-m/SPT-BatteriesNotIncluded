using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.External;

public class Fika : AbstractExternalMod
{
    protected override string Guid { get; } = "com.fika.core";

    protected override ModulePatch[] Patches { get; } = [];

    public bool IsServer { get; set; }
    public bool IsClient => IsPresent && !IsServer;
}
