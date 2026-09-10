using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly HashSet<string> _available = new(StringComparer.OrdinalIgnoreCase);

    public List<(string Executable, IReadOnlyList<string> Arguments, TimeSpan? Timeout)> Invocations { get; } = [];

    public ProcessResult Result { get; set; } = new(0, string.Empty, string.Empty);

    /// <summary>Optional side effect invoked when a process runs.</summary>
    public Action? OnRun { get; set; }

    /// <summary>Optional per-command result; null falls back to <see cref="Result"/>.</summary>
    public Func<string, IReadOnlyList<string>, ProcessResult?>? Respond { get; set; }

    public FakeProcessRunner WithAvailable(params string[] executables)
    {
        foreach (var exe in executables)
        {
            _available.Add(exe);
        }

        return this;
    }

    public bool Exists(string executable) => _available.Contains(executable);

    public Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        Invocations.Add((executable, arguments, timeout));
        OnRun?.Invoke();
        return Task.FromResult(Respond?.Invoke(executable, arguments) ?? Result);
    }
}
