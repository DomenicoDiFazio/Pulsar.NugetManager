using Pulsar.NugetManager.Models;

namespace Pulsar.NugetManager.Services.Interfaces;

public interface IDllScannerService
{
    Task<List<DllInfo>> ScanFolderAsync(string folderPath);
}
