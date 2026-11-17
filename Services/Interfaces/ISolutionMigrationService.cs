using Pulsar.NugetManager.Models;

namespace Pulsar.NugetManager.Services.Interfaces;

public interface ISolutionMigrationService
{
    Task<List<ProjectReferenceInfo>> AnalyzeSolutionAsync(string solutionPath);
}
