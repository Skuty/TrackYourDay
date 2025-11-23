// Stub entry point for non-Windows builds where MAUI is not supported
// This file is only compiled when building on non-Windows platforms
public class Program
{
    public static void Main(string[] args)
    {
        throw new PlatformNotSupportedException("TrackYourDay.MAUI is only supported on Windows.");
    }
}
