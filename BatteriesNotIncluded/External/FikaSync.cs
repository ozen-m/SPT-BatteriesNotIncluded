using System;

namespace BatteriesNotIncluded.External;

public class FikaSync : AbstractExternalMod
{
    protected override string Guid { get; } = "com.ozen.batteriesnotincluded.fikasync";
    protected override Version MinimumVersion { get; } = new(1, 0, 2);
}
