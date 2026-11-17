namespace Pulsar.NugetManager.Helpers;

public static class FrameworkHelper
{
    public static string ToNugetFramework(string frameworkName)
    {
        if (string.IsNullOrWhiteSpace(frameworkName))
        {
            return "net48";
        }

        frameworkName = frameworkName.ToLower();

        if (frameworkName.Contains(".netframework") &&
            frameworkName.Contains("4.8"))
        {
            return "net48";
        }

        if (frameworkName.Contains(".netframework") &&
            frameworkName.Contains("4.7.2"))
        {
            return "net472";
        }

        if (frameworkName.Contains(".netstandard") &&
            frameworkName.Contains("2.0"))
        {
            return "netstandard2.0";
        }

        if (frameworkName.Contains(".netcoreapp") &&
            frameworkName.Contains("8.0"))
        {
            return "net8.0";
        }

        if (frameworkName.Contains(".netcoreapp") &&
            frameworkName.Contains("9.0"))
        {
            return "net9.0";
        }

        return "net48";
    }
}
