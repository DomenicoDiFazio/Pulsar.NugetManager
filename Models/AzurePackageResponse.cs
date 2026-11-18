namespace Pulsar.NugetManager.Models;

public class AzurePackageResponse
{
    public int Count { get; set; }

    public List<AzurePackageDto> Value { get; set; } = [];
}
