namespace Pulsar.NugetManager.Models;

public class PackageMatchInfo
{
    public string ReferenceName { get; set; } = string.Empty;

    public string FeedName { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string PackageVersion { get; set; } = string.Empty;

    public bool MatchFound { get; set; }

    public string DllVersion { get; set; } = string.Empty;

    public string Status =>
        MatchFound
            ? "Match"
            : "Non trovato";
}
