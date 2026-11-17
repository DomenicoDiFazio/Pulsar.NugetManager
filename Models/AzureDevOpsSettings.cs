namespace Pulsar.NugetManager.Models;

public class AzureDevOpsSettings
{
    public string Organization { get; set; } = string.Empty;

    public string PersonalAccessToken { get; set; } = string.Empty;

    public string DefaultFeed { get; set; } = string.Empty;
}
