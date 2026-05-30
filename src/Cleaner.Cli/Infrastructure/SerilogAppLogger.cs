using Cleaner.Core.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Cleaner.Cli.Infrastructure;

/// <summary>
/// File-backed <see cref="IAppLogger"/> built on Serilog. Configured entirely in code (no
/// reflection-based <c>Serilog.Settings.Configuration</c>) so it stays Native-AOT clean. Writes to a
/// size-rolling <c>cleaner.log</c> under <see cref="IEnvironmentService.LogDirectory"/>.
/// </summary>
public sealed class SerilogAppLogger : IAppLogger, IDisposable
{
    private readonly Logger _logger;

    public SerilogAppLogger(IEnvironmentService environment)
    {
        var directory = environment.LogDirectory;
        Directory.CreateDirectory(directory);
        LogFilePath = Path.Combine(directory, "cleaner.log");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                LogFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 5 * 1024 * 1024,
                retainedFileCountLimit: 5,
                shared: true)
            .CreateLogger();
    }

    public string LogFilePath { get; }

    public void Info(string message) => _logger.Write(LogEventLevel.Information, message);

    public void Warn(string message) => _logger.Write(LogEventLevel.Warning, message);

    public void Error(string message, Exception? exception = null) =>
        _logger.Write(LogEventLevel.Error, exception, message);

    public void Dispose() => _logger.Dispose();
}
