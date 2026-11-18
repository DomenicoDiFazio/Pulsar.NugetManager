namespace Pulsar.NugetManager.Services.Interfaces;

public interface ILoggingService
{
    void Info(string message);

    void Error(string message);

    void Error(string message, Exception ex);
}
