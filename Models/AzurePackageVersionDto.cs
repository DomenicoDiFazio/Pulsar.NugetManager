namespace Pulsar.NugetManager.Models;

public class AzurePackageVersionDto
{
    public string Version { get; set; } = string.Empty;

    public string NormalizedVersion { get; set; } = string.Empty;

    public bool IsLatest { get; set; }
}