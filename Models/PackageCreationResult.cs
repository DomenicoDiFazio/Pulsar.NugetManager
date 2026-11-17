namespace Pulsar.NugetManager.Models;

public class PackageCreationResult
{
    public string PackageName { get; set; } = string.Empty;

    public string PackagePath { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }
}
