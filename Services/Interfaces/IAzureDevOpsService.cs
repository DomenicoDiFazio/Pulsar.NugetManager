using Pulsar.NugetManager.Models;

namespace Pulsar.NugetManager.Services.Interfaces;

public interface IAzureDevOpsService
{
    Task<AzureDevOpsSettings> GetSettingsAsync();

    Task SaveSettingsAsync(AzureDevOpsSettings settings);

    Task<List<AzureFeedInfo>> GetFeedsAsync();

    Task PublishPackageAsync(string packagePath, string feedName);

    Task<List<AzurePackageInfo>> GetAllPackagesAsync();

    Task<PackageMatchInfo>FindPackageMatchAsync(string referenceName, string dllVersion);
}
