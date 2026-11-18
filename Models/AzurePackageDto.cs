namespace Pulsar.NugetManager.Models;

public class AzurePackageDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<AzurePackageVersionDto> Versions { get; set; } = [];
}
