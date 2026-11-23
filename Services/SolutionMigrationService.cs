using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Pulsar.NugetManager.Services;

public class SolutionMigrationService : ISolutionMigrationService
{
    private readonly ILoggingService _logger;

    public SolutionMigrationService(ILoggingService logger)
    {
        _logger = logger;
    }

    public async Task<List<ProjectReferenceInfo>> AnalyzeSolutionAsync(string solutionFile, string dllFolder)
    {

        _logger.Info($"Analyzing solution file: {solutionFile}");

        var results = new List<ProjectReferenceInfo>();

        if (!File.Exists(solutionFile))
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

                var referenceName =
                    (reference.Attribute("Include")?.Value
                        ?? string.Empty)
                        .Split(',')[0]
                        .Trim();

                var dllVersion =
                    GetDllVersion(
                        dllFolder,
                        referenceName);

                results.Add(new ProjectReferenceInfo
                {
                    SolutionName =
                        Path.GetFileNameWithoutExtension(solutionFile),

                    ProjectName =
                        Path.GetFileNameWithoutExtension(projectFile),

                    ProjectPath =
                        projectFile,

                    ReferenceName =
                       referenceName,

                    DllVersion =
                            dllVersion,

                    ReferencePath =
                        hintPath.Value
                });
            }
        }

        return results;
    }

    public async Task<List<MigrationPreviewInfo>>
    GeneratePreviewAsync(List<ProjectReferenceInfo> references, List<PackageMatchInfo> matches)
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
                        """,

                    DllVersion = reference.DllVersion
                });
        }

        return await Task.FromResult(previews);
    }

    public async Task<List<MigrationResult>>
    MigrateAsync( List<MigrationPreviewInfo> previews)
    {
        var results = new List<MigrationResult>();

        var projects =
            previews
                .GroupBy(x => x.ProjectPath);

        foreach (var project in projects)
        {
            var projectPath = project.Key;

            try
            {
                
                _logger.Info($"Migrating project: {projectPath}");

                var backupFolder =
                    Path.Combine(
                        Path.GetDirectoryName(projectPath)!,
                        "Backup");

                Directory.CreateDirectory(
                    backupFolder);

                File.Copy(
                    projectPath,
                    Path.Combine(
                        backupFolder,
                        Path.GetFileName(projectPath)),
                    true);

                var document =
                    XDocument.Load(projectPath);

                foreach (var item in project)
                {
                    RemoveReference(
                        document,
                        item.ReferenceName);

                    AddPackageReference(
                        document,
                        item.PackageName,
                        item.PackageVersion);
                }

                document.Save(projectPath);

                _logger.Info($"Migration completed: {projectPath}");

                foreach (var item in project)
                {
                    results.Add(
                        new MigrationResult
                        {
                            ProjectName =
                                item.ProjectName,

                            PackageName =
                                item.PackageName,

                            Version =
                                item.PackageVersion,

                            Success = true,

                            Message =
                                "Migrato con successo"
                        });
                }
            }
            catch (Exception ex)
            {

                _logger.Error($"Migration failed: {projectPath}", ex);

                foreach (var item in project)
                {
                    results.Add(
                        new MigrationResult
                        {
                            ProjectName =
                                item.ProjectName,

                            PackageName =
                                item.PackageName,

                            Version =
                                item.PackageVersion,

                            Success = false,

                            Message = ex.Message
                        });
                }
            }
        }

        return await Task.FromResult(results);
    }

    private static void RemoveReference(XDocument document, string referenceName)
    {
        var references =
            document.Descendants()
                .Where(x =>
                    x.Name.LocalName == "Reference");

        var reference =
            references.FirstOrDefault(x =>
                (x.Attribute("Include")?.Value ?? "")
                    .StartsWith(
                        referenceName,
                        StringComparison.OrdinalIgnoreCase));

        reference?.Remove();
    }

    private static void AddPackageReference(XDocument document, string packageName, string version)
    {
        var project =
            document.Root;

        if (project is null)
        {
            return;
        }

        var itemGroup =
            project.Elements()
                .FirstOrDefault(x =>
                    x.Name.LocalName == "ItemGroup" &&
                    x.Elements()
                        .Any(e =>
                            e.Name.LocalName ==
                            "PackageReference"));

        if (itemGroup is null)
        {
            itemGroup =
                new XElement(
                    project.Name.Namespace +
                    "ItemGroup");

            project.Add(itemGroup);
        }

        itemGroup.Add(
            new XElement(
                project.Name.Namespace +
                "PackageReference",
                new XAttribute(
                    "Include",
                    packageName),
                new XAttribute(
                    "Version",
                    version)));
    }

    private static string GetDllVersion(
    string dllFolder,
    string referenceName)
    {
        try
        {
            var dllPath =
                Path.Combine(
                    dllFolder,
                    $"{referenceName}.dll");

            if (!File.Exists(dllPath))
            {
                return "N/A";
            }

            var assemblyName =
                AssemblyName.GetAssemblyName(
                    dllPath);

            return assemblyName.Version?.ToString()
                   ?? "N/A";
        }
        catch
        {
            return "N/A";
        }
    }
}