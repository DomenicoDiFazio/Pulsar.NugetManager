namespace Pulsar.NugetManager.Models;

public class MigrationPreviewInfo
{
    public string SolutionName { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string ProjectPath { get; set; } = string.Empty;

    public string ReferenceName { get; set; } = string.Empty;

    public string HintPath { get; set; } = string.Empty;

    public string FeedName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string PackageVersion { get; set; } = string.Empty;

    public string CurrentReferenceXml { get; set; } = string.Empty;

    public string NewReferenceXml { get; set; } = string.Empty;

    public string DllVersion { get; set; } = string.Empty;
}
