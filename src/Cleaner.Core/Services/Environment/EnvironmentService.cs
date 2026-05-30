using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IEnvironmentService"/>
public sealed partial class EnvironmentService : IEnvironmentService
{
    private readonly Lazy<bool> _isElevated;

    public EnvironmentService()
    {
        Os = OperatingSystem.IsWindows() ? OsPlatform.Windows
            : OperatingSystem.IsMacOS() ? OsPlatform.MacOs
            : OperatingSystem.IsLinux() ? OsPlatform.Linux
            : OsPlatform.Unknown;

        _isElevated = new Lazy<bool>(DetectElevation);
    }

    public OsPlatform Os { get; }

    public bool IsWindows => Os == OsPlatform.Windows;

    public bool IsMacOs => Os == OsPlatform.MacOs;

    public bool IsLinux => Os == OsPlatform.Linux;

    public bool IsElevated => _isElevated.Value;

    public string HomeDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);

    public string TempDirectory => Path.GetTempPath();

    public string LocalAppDataDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

    public string AppDataDirectory =>
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);

    public string CacheDirectory
    {
        get
        {
            if (IsWindows)
            {
                return LocalAppDataDirectory;
            }

            if (IsMacOs)
            {
                return Path.Combine(HomeDirectory, "Library", "Caches");
            }

            var xdg = GetEnvironmentVariable("XDG_CACHE_HOME");
            return !string.IsNullOrWhiteSpace(xdg) ? xdg : Path.Combine(HomeDirectory, ".cache");
        }
    }

    public string? WindowsDirectory =>
        IsWindows ? Environment.GetFolderPath(Environment.SpecialFolder.Windows, Environment.SpecialFolderOption.DoNotVerify) : null;

    public string LogDirectory => HomePath(".cleaner", "logs");

    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    public string HomePath(params string[] segments) => Path.Combine([HomeDirectory, .. segments]);

    private bool DetectElevation()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return IsUserAnAdmin();
            }

            // Unix: effective uid 0 is root.
            return GetEuid() == 0;
        }
        catch
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    [LibraryImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsUserAnAdmin();

    [LibraryImport("libc", EntryPoint = "geteuid")]
    private static partial uint GetEuid();
}
