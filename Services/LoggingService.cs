using NLog;
using Pulsar.NugetManager.Services.Interfaces;

namespace Pulsar.NugetManager.Services;

public class LoggingService : ILoggingService
{
    private static readonly Logger Logger =
        LogManager.GetCurrentClassLogger();

    public void Info(string message)
    {
        Logger.Info(message);
    }

    public void Error(string message)
    {
        Logger.Error(message);
    }

    public void Error(string message, Exception ex)
    {
        Logger.Error(ex, message);
    }
}
