namespace Pulsar.NugetManager.Models;

public class ProjectReferenceInfo
{
    public string SolutionName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    public string ProjectPath { get; set; } = string.Empty;

    public string ReferenceName { get; set; } = string.Empty;

    public string ReferencePath { get; set; } = string.Empty;
}
