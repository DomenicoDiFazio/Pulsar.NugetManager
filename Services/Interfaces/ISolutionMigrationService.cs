using Pulsar.NugetManager.Models;

namespace Pulsar.NugetManager.Services.Interfaces;

public interface ISolutionMigrationService
{
    Task<List<ProjectReferenceInfo>> AnalyzeSolutionAsync(string solutionPath);

    Task<List<MigrationPreviewInfo>>
    GeneratePreviewAsync(List<ProjectReferenceInfo> references, List<PackageMatchInfo> matches);

}
