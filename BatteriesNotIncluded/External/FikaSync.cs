using System;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.External;

public class FikaSync : AbstractExternalMod
{
    protected override string Guid { get; } = "com.ozen.batteriesnotincluded.fikasync";
    public override Version MinimumVersion { get; } = new(1, 0, 2);

    protected override ModulePatch[] Patches { get; } = [];
}
