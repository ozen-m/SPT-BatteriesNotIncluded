namespace BatteriesNotIncluded.External;

public static class Fika
{
    public static bool IsFikaPresent { get; set; }
    public static bool IsFikaSyncPresent { get; set; }
    public static bool IsFikaServer { get; set; }
    public static bool IsFikaClient => IsFikaPresent && !IsFikaServer;
}
