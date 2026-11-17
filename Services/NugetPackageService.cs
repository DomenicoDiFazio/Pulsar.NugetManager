using NuGet.Packaging;
using NuGet.Versioning;
using Pulsar.NugetManager.Helpers;
using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;

namespace Pulsar.NugetManager.Services;

public class NugetPackageService : INugetPackageService
{
    public async Task<List<PackageCreationResult>> CreatePackagesAsync(
        IEnumerable<DllInfo> dlls,
        string outputFolder)
    {
        var results = new List<PackageCreationResult>();

        Directory.CreateDirectory(outputFolder);

        foreach (var dll in dlls.Where(x => x.Selected))
        {
            try
            {
                var packageId = Path.GetFileNameWithoutExtension(dll.Name);

                var packageVersion = dll.Version;

                var builder = new PackageBuilder();

                builder.Id = packageId;

                builder.Version = NuGetVersion.Parse(packageVersion);

                builder.Description =
                    $"Auto generated package for {packageId}";

                builder.Authors.Add("Pulsar");

                var framework =
                    FrameworkHelper.ToNugetFramework(
                        dll.TargetFramework);

                builder.Files.Add(
                    new PhysicalPackageFile
                    {
                        SourcePath = dll.FullPath,
                        TargetPath = $"lib/{framework}/{dll.Name}"
                    });

                var packageFileName =
                    $"{packageId}.{packageVersion}.nupkg";

                var packagePath =
                    Path.Combine(outputFolder, packageFileName);

                await using var stream =
                    File.Create(packagePath);

                builder.Save(stream);

                results.Add(new PackageCreationResult
                {
                    PackageName = packageId,
                    PackagePath = packagePath,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                results.Add(new PackageCreationResult
                {
                    PackageName = dll.Name,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        return results;
    }
}