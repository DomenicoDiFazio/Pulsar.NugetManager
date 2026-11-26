namespace Pulsar.NugetManager.Models;

public class NugetPackageFile
{
    public string FileName { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool Selected { get; set; } = true;
}
