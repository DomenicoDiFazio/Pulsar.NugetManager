namespace Pulsar.NugetManager.Models;

public class NugetPackageInfo
{
    public string PackageId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string DllPath { get; set; } = string.Empty;

    public string OutputPath { get; set; } = string.Empty;

    public bool Selected { get; set; }
}

