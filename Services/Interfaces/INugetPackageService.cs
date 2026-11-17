using Pulsar.NugetManager.Models;

namespace Pulsar.NugetManager.Services.Interfaces;

public interface INugetPackageService
{
    Task<List<PackageCreationResult>> CreatePackagesAsync(
        IEnumerable<DllInfo> dlls,
        string outputFolder);
}
