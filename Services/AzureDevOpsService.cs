using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;
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
}
