namespace Pulsar.NugetManager.Models;

public class DllInfo
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public long FileSize { get; set; }
}