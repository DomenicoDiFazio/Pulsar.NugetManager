using System.Xml.Linq;
using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;

namespace Pulsar.NugetManager.Services;

public class SolutionMigrationService : ISolutionMigrationService
{
    public async Task<List<ProjectReferenceInfo>> AnalyzeSolutionAsync(
        string solutionPath)
    {
        var results = new List<ProjectReferenceInfo>();

        var solutionFolder =
            Path.GetDirectoryName(solutionPath)!;

        var projectFiles =
            Directory.GetFiles(
                solutionFolder,
                "*.csproj",
                SearchOption.AllDirectories);

        foreach (var projectFile in projectFiles)
        {
            var document = XDocument.Load(projectFile);

            var references =
                document.Descendants("Reference");

            foreach (var reference in references)
            {
                var hintPath =
                    reference.Element("HintPath");

                if (hintPath is null)
                {
                    continue;
                }

                results.Add(new ProjectReferenceInfo
                {
                    ProjectName =
                        Path.GetFileNameWithoutExtension(
                            projectFile),

                    ProjectPath = projectFile,

                    ReferenceName =
                        reference.Attribute("Include")?.Value
                        ?? string.Empty,

                    ReferencePath =
                        hintPath.Value
                });
            }
        }

        return await Task.FromResult(results);
    }
}
