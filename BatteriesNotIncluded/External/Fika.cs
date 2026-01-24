namespace BatteriesNotIncluded.External;

public class Fika : AbstractExternalMod
{
    protected override string Guid { get; } = "com.fika.core";

    public bool IsServer { get; set; }
    public bool IsClient => IsPresent && !IsServer;
}
