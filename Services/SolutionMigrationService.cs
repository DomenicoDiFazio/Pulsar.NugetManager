using System.Text.RegularExpressions;
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

        string solutionFile;

        if (Directory.Exists(solutionPath))
        {
            var slnFiles = Directory.GetFiles(
                solutionPath,
                "*.sln",
                SearchOption.TopDirectoryOnly);

            if (slnFiles.Length == 0)
            {
                return results;
            }

            solutionFile = slnFiles[0];
        }
        else
        {
            return results;
        }

        var solutionFolder =
            Path.GetDirectoryName(solutionFile)!;

        var slnLines =
            await File.ReadAllLinesAsync(solutionFile);


        var projectFiles = new List<string>();

        foreach (var line in slnLines)
        {
            if (!line.StartsWith("Project("))
            {
                continue;
            }

            var match = Regex.Match(
                line,
                "\"([^\"]+\\.csproj)\"",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                continue;
            }

            var relativeProjectPath =
                match.Groups[1].Value;

            var fullProjectPath =
                Path.GetFullPath(
                    Path.Combine(
                        solutionFolder,
                        relativeProjectPath));

            if (File.Exists(fullProjectPath))
            {
                projectFiles.Add(fullProjectPath);
            }
        }

        foreach (var projectFile in projectFiles)
        {
            var document = XDocument.Load(projectFile);

            var references = document
                  .Descendants()
                  .Where(x => x.Name.LocalName == "Reference");

            foreach (var reference in references)
            {
                var hintPath = reference
                     .Elements()
                     .FirstOrDefault(x => x.Name.LocalName == "HintPath");

                if (hintPath is null)
                {
                    continue;
                }

                results.Add(new ProjectReferenceInfo
                {
                    SolutionName =
                        Path.GetFileNameWithoutExtension(solutionFile),

                    ProjectName =
                        Path.GetFileNameWithoutExtension(projectFile),

                    ProjectPath =
                        projectFile,

                    ReferenceName =
                        reference.Attribute("Include")?.Value
                        ?? string.Empty,

                    ReferencePath =
                        hintPath.Value
                });
            }
        }

        return results;
    }

    public async Task<List<MigrationPreviewInfo>>
    GeneratePreviewAsync(
        List<ProjectReferenceInfo> references,
        List<PackageMatchInfo> matches)
    {
        var previews =
            new List<MigrationPreviewInfo>();

        foreach (var reference in references)
        {
            var match =
                matches.FirstOrDefault(x =>
                    x.ReferenceName == reference.ReferenceName &&
                    x.MatchFound);

            if (match is null)
            {
                continue;
            }

            previews.Add(
                new MigrationPreviewInfo
                {
                    SolutionName =
                        reference.SolutionName,

                    ProjectName =
                        reference.ProjectName,

                    ProjectPath =
                        reference.ProjectPath,

                    ReferenceName =
                        reference.ReferenceName,

                    HintPath =
                        reference.ReferencePath,

                    FeedName =
                        match.FeedName,

                    PackageName =
                        match.PackageName,

                    PackageVersion =
                        match.PackageVersion,

                    CurrentReferenceXml =
                            $"""
                        <Reference Include="{reference.ReferenceName}">
                            <HintPath>{reference.ReferencePath}</HintPath>
                        </Reference>
                        """,

                                            NewReferenceXml =
                            $"""
                        <PackageReference Include="{match.PackageName}"
                                          Version="{match.PackageVersion}" />
                        """
                });
        }

        return await Task.FromResult(previews);
    }
}