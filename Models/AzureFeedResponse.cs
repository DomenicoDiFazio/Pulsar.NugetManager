namespace Pulsar.NugetManager.Models;

public class AzureFeedResponse
{
    public int Count { get; set; }

    public List<AzureFeedInfo> Value { get; set; } = [];
}
