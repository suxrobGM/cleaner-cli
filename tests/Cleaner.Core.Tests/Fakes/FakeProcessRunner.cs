using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

/// <summary>Records process invocations and returns a canned result without spawning anything.</summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly HashSet<string> _available = new(StringComparer.OrdinalIgnoreCase);

    public List<(string Executable, IReadOnlyList<string> Arguments)> Invocations { get; } = [];

    public ProcessResult Result { get; set; } = new(0, string.Empty, string.Empty);

    /// <summary>Optional side effect invoked when a process "runs" (e.g. to mutate the fake FS).</summary>
    public Action? OnRun { get; set; }

    public FakeProcessRunner WithAvailable(params string[] executables)
    {
        foreach (var exe in executables)
        {
            _available.Add(exe);
        }

        return this;
    }

    public bool Exists(string executable) => _available.Contains(executable);

    public Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        Invocations.Add((executable, arguments));
        OnRun?.Invoke();
        return Task.FromResult(Result);
    }
}
