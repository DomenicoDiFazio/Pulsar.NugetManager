using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace Pulsar.NugetManager.Services;

public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly string _settingsFile =
        Path.Combine(
            AppContext.BaseDirectory,
            "azuredevops-settings.json");

    private List<AzurePackageInfo> _packageCache = [];

    private readonly ILoggingService _logger;

    public AzureDevOpsService(
        ILoggingService logger)
    {
        _logger = logger;
    }

    public async Task<AzureDevOpsSettings> GetSettingsAsync()
    {
        if (!File.Exists(_settingsFile))
        {
            return new AzureDevOpsSettings();
        }

        var json =
            await File.ReadAllTextAsync(_settingsFile);

        var settings =
            JsonSerializer.Deserialize<AzureDevOpsSettings>(json)
            ?? new AzureDevOpsSettings();

        settings.PersonalAccessToken =
            TryDecrypt(settings.PersonalAccessToken);

        return settings;
    }

    public async Task SaveSettingsAsync(
    AzureDevOpsSettings settings)
    {
        var settingsToSave =
            new AzureDevOpsSettings
            {
                Organization = settings.Organization,
                PersonalAccessToken =
                    Encrypt(settings.PersonalAccessToken)
            };

        var json =
            JsonSerializer.Serialize(
                settingsToSave,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            _settingsFile,
            json);
    }

    public async Task<List<AzureFeedInfo>> GetFeedsAsync()
    {
        var settings = await GetSettingsAsync();

        using var client = new HttpClient();

        var token = Convert.ToBase64String(
            Encoding.ASCII.GetBytes(
                $":{settings.PersonalAccessToken}"));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                token);

        var url =
            $"https://feeds.dev.azure.com/{settings.Organization}/_apis/packaging/feeds?api-version=7.1-preview.1";

        var response = await client.GetAsync(url);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"HTTP {(int)response.StatusCode}\n\n{content}");
        }

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content.ReadAsStringAsync();

        var result =
            JsonSerializer.Deserialize<AzureFeedResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return result?.Value ?? [];
    }

    public async Task PublishPackageAsync(
    string packagePath,
    string feedName)
    {
        var settings = await GetSettingsAsync();

        var feedUrl =
            $"https://pkgs.dev.azure.com/{settings.Organization}" +
            $"/_packaging/{feedName}" +
            $"/nuget/v3/index.json";

        var process = new Process();

        process.StartInfo.FileName = "dotnet";

        process.StartInfo.Arguments =
            $"nuget push \"{packagePath}\" " +
            $"--source \"{feedUrl}\" " +
            $"--api-key az";

        process.StartInfo.RedirectStandardOutput = true;

        process.StartInfo.RedirectStandardError = true;

        process.StartInfo.UseShellExecute = false;

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(error)
                ? output
                : error;

            _logger.Error(
                $"Package {packagePath} upload failed. ExitCode={process.ExitCode}. Message={message}");

            throw new Exception(message);
        }

        _logger.Info($"Package {packagePath} uploaded ");

    }

    public async Task<List<AzurePackageInfo>> GetAllPackagesAsync()
    {
        if (_packageCache.Any())
        {
            return _packageCache;
        }

        var feeds = await GetFeedsAsync();

        foreach (var feed in feeds)
        {
            var packages =
                await GetPackagesForFeedAsync(feed);

            _packageCache.AddRange(packages);
        }

        return _packageCache;
    }

    private async Task<List<AzurePackageInfo>> GetPackagesForFeedAsync(AzureFeedInfo feed)
    {
        var settings = await GetSettingsAsync();

        using var client = new HttpClient();

        var token =
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes(
                    $":{settings.PersonalAccessToken}"));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                token);

        var url =
            $"https://feeds.dev.azure.com/" +
            $"{settings.Organization}" +
            $"/_apis/packaging/Feeds/" +
            $"{feed.Id}/packages" +
            $"?api-version=7.1-preview.1";

        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var json =
            await response.Content.ReadAsStringAsync();

        var result =
            JsonSerializer.Deserialize<AzurePackageResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result is null)
        {
            return [];
        }

        var packages = new List<AzurePackageInfo>();

        foreach (var package in result.Value)
        {
            foreach (var version in package.Versions)
            {
                packages.Add(
                    new AzurePackageInfo
                    {
                        FeedName = feed.Name,
                        PackageId = package.Name,
                        Version = version.Version
                    });
            }
        }

        return packages;
    }

    public async Task<PackageMatchInfo> FindPackageMatchAsync(
    string referenceName,
    string dllVersion)
    {

        if (string.IsNullOrWhiteSpace(dllVersion) || dllVersion == "N/A")
        {
            return new PackageMatchInfo
            {
                ReferenceName = referenceName,
                DllVersion = dllVersion,
                MatchFound = false
            };
        }

        var packages =
            await GetAllPackagesAsync();

        var normalizedDllVersion =
            NormalizeVersion(dllVersion);

        var match =
            packages.FirstOrDefault(x =>
                string.Equals(
                    x.PackageId,
                    referenceName,
                    StringComparison.OrdinalIgnoreCase)

                &&

                string.Equals(
                    NormalizeVersion(x.Version),
                    normalizedDllVersion,
                    StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            _logger.Info(
                $"Package not found: {referenceName} ({dllVersion})");

            return new PackageMatchInfo
            {
                ReferenceName = referenceName,
                DllVersion = dllVersion,
                MatchFound = false
            };
        }

        _logger.Info(
            $"Package match found: {referenceName} ({dllVersion}) -> {match.PackageId} ({match.Version})");

        return new PackageMatchInfo
        {
            ReferenceName = referenceName,
            DllVersion = dllVersion,
            FeedName = match.FeedName,
            PackageName = match.PackageId,
            PackageVersion = match.Version,
            MatchFound = true
        };
    }

    private static string NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        if (!Version.TryParse(version, out var parsedVersion))
        {
            return version.Trim();
        }

        if (parsedVersion.Build > 0)
        {
            return
                $"{parsedVersion.Major}." +
                $"{parsedVersion.Minor}." +
                $"{parsedVersion.Build}";
        }

        return
            $"{parsedVersion.Major}." +
            $"{parsedVersion.Minor}";
    }

    private static string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        var bytes =
            Encoding.UTF8.GetBytes(plainText);

        var encryptedBytes =
            ProtectedData.Protect(
                bytes,
                null,
                DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(encryptedBytes);
    }

    private static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            return string.Empty;
        }

        var encryptedBytes =
            Convert.FromBase64String(encryptedText);

        var decryptedBytes =
            ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static string TryDecrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            return Decrypt(value);
        }
        catch
        {
            return value;
        }
    }
}
