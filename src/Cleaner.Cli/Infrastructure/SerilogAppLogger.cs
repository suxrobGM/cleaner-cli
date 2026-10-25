using Cleaner.Core.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Cleaner.Cli.Infrastructure;

/// <summary>Native-AOT-safe, file-backed Serilog implementation of <see cref="IAppLogger"/>.</summary>
/// <remarks>
/// <c>cleaner.log</c> rolls and keeps run summaries; each clean also writes every path it removed or
/// skipped to its own <c>runs/run-&lt;timestamp&gt;.log</c>, the only record of what was deleted.
/// </remarks>
public sealed class SerilogAppLogger : IAppLogger, IDisposable
{
    private const string Template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>Run logs kept; older ones are deleted when a new run starts.</summary>
    private const int RunLogsKept = 30;

    private readonly Logger _logger;
    private readonly string _runDirectory;
    private Logger? _runLogger;

    public SerilogAppLogger(IEnvironmentService environment)
    {
        var directory = environment.LogDirectory;
        Directory.CreateDirectory(directory);
        LogFilePath = Path.Combine(directory, "cleaner.log");
        _runDirectory = Path.Combine(directory, "runs");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                LogFilePath,
                outputTemplate: Template,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 5 * 1024 * 1024,
                retainedFileCountLimit: 5,
                shared: true)
            .CreateLogger();
    }

    public string LogFilePath { get; }

    public string? RunLogFilePath { get; private set; }

    public void Info(string message) => Write(LogEventLevel.Information, message);

    public void Warn(string message) => Write(LogEventLevel.Warning, message);

    public void Error(string message, Exception? exception = null) => Write(LogEventLevel.Error, message, exception);

    public void Detail(string message) =>
        (_runLogger ?? _logger).Write(LogEventLevel.Information, message);

    public IDisposable BeginRun()
    {
        EndRun();
        Directory.CreateDirectory(_runDirectory);
        PruneRunLogs();

        RunLogFilePath = Path.Combine(_runDirectory, $"run-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        _runLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(RunLogFilePath, outputTemplate: Template, shared: true)
            .CreateLogger();
        return new RunScope(this);
    }

    public void Dispose()
    {
        EndRun();
        _logger.Dispose();
    }

    private void Write(LogEventLevel level, string message, Exception? exception = null)
    {
        _logger.Write(level, exception, message);
        _runLogger?.Write(level, exception, message);
    }

    private void EndRun()
    {
        _runLogger?.Dispose();
        _runLogger = null;
        RunLogFilePath = null;
    }

    private void PruneRunLogs()
    {
        try
        {
            foreach (var old in new DirectoryInfo(_runDirectory).GetFiles("run-*.log")
                         .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                         .Skip(RunLogsKept - 1))
            {
                old.Delete();
            }
        }
        catch (IOException)
        {
            // Pruning is housekeeping; a locked old log can wait for the next run.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class RunScope(SerilogAppLogger owner) : IDisposable
    {
        public void Dispose() => owner.EndRun();
    }
}
