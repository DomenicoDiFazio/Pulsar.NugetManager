using Pulsar.NugetManager.Models;
using Pulsar.NugetManager.Services.Interfaces;
using System.Reflection;

namespace Pulsar.NugetManager.Services;

public class DllScannerService : IDllScannerService
{
    public Task<List<DllInfo>> ScanFolderAsync(string folderPath)
    {
        var result = new List<DllInfo>();

        if (!Directory.Exists(folderPath))
        {
            return Task.FromResult(result);
        }

        var dllFiles = Directory.GetFiles(
            folderPath,
            "*.dll",
            SearchOption.TopDirectoryOnly);

        foreach (var dllFile in dllFiles)
        {
            var assemblyName = AssemblyName.GetAssemblyName(dllFile);
            var fileInfo = new FileInfo(dllFile);

            result.Add(new DllInfo
            {
                Name = Path.GetFileName(dllFile),
                Version = assemblyName.Version?.ToString() ?? "N/A",
                FullPath = dllFile,
                FileSize = fileInfo.Length
            });
        }

        return Task.FromResult(result);
    }
}
