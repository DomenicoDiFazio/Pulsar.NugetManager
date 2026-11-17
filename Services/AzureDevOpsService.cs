using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Pulsar.NugetManager.Services;

public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly string _settingsFile =
        Path.Combine(
            AppContext.BaseDirectory,
            "azuredevops-settings.json");

    public async Task<AzureDevOpsSettings> GetSettingsAsync()
    {
        if (!File.Exists(_settingsFile))
        {
            return new AzureDevOpsSettings();
        }

        var json = await File.ReadAllTextAsync(_settingsFile);

        return JsonSerializer.Deserialize<AzureDevOpsSettings>(json)
               ?? new AzureDevOpsSettings();
    }

    public async Task SaveSettingsAsync(
        AzureDevOpsSettings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
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

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error =
                await process.StandardError.ReadToEndAsync();

            throw new Exception(error);
        }
    }
}
