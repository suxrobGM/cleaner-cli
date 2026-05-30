using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

/// <summary>A configurable <see cref="IEnvironmentService"/> with a deterministic, Unix-like layout.</summary>
public sealed class FakeEnvironment : IEnvironmentService
{
    private readonly Dictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);

    public OsPlatform Os { get; set; } = OsPlatform.Linux;

    public bool IsWindows => Os == OsPlatform.Windows;

    public bool IsMacOs => Os == OsPlatform.MacOs;

    public bool IsLinux => Os == OsPlatform.Linux;

    public bool IsElevated { get; set; }

    public string HomeDirectory { get; set; } = "/home/test";

    public string TempDirectory { get; set; } = "/tmp";

    public string LocalAppDataDirectory { get; set; } = "/home/test/.local/share";

    public string AppDataDirectory { get; set; } = "/home/test/.config";

    public string CacheDirectory { get; set; } = "/home/test/.cache";

    public string? WindowsDirectory { get; set; }

    public string? GetEnvironmentVariable(string name) => _vars.GetValueOrDefault(name);

    public FakeEnvironment SetVariable(string name, string value)
    {
        _vars[name] = value;
        return this;
    }

    public string HomePath(params string[] segments) => Path.Combine([HomeDirectory, .. segments]);
}
