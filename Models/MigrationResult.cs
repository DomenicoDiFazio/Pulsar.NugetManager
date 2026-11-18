namespace Pulsar.NugetManager.Models;

public class MigrationResult
{
    public string ProjectName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
