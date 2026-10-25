using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// When a folder is a project's build output rather than something that only shares its name. A
/// folder qualifies only when its parent is a project of the right kind and the folder carries
/// proof it was generated there; an installed tool's <c>bin</c> or an editor extension's
/// <c>node_modules</c> has neither.
/// </summary>
internal static class BuildOutput
{
    private static readonly string[] DotnetProjects = ["*.csproj", "*.fsproj", "*.vbproj"];

    private static readonly string[] JsLockfiles =
        ["package-lock.json", "npm-shrinkwrap.json", "yarn.lock", "pnpm-lock.yaml", "bun.lock", "bun.lockb"];

    private static readonly string[] JsInstallStamps =
        [".package-lock.json", ".modules.yaml", ".yarn-state.yml", ".yarn-integrity"];

    private static readonly string[] PythonProjects =
        ["pyproject.toml", "setup.py", "setup.cfg", "Pipfile", "poetry.lock", "uv.lock", "requirements.txt"];

    private static readonly string[] BuildSystems =
    [
        "build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts",
        "gradlew", "pom.xml", "CMakeLists.txt", "meson.build",
    ];

    /// <summary>Framework output folders whose names are specific enough to need only a package.json.</summary>
    private static readonly HashSet<string> JsToolFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".next", ".nuxt", ".svelte-kit", ".astro", ".turbo", ".parcel-cache", ".vite", ".angular",
    };

    /// <summary>Caches that tag themselves with <c>CACHEDIR.TAG</c>.</summary>
    private static readonly HashSet<string> TaggedCaches = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pytest_cache", ".mypy_cache", ".ruff_cache",
    };

    /// <summary>True when <paramref name="directory"/> is build output its project can regenerate.</summary>
    public static bool Matches(IFileSystemService fs, string directory)
    {
        var name = PathComparison.Name(directory);
        if (PathComparison.Parent(directory) is not { } parent)
        {
            return false;
        }

        bool Has(string file) => fs.FileExists(Path.Combine(parent, file));
        bool Inside(string entry) =>
            fs.FileExists(Path.Combine(directory, entry)) || fs.DirectoryExists(Path.Combine(directory, entry));

        switch (name.ToLowerInvariant())
        {
            case "bin":
                // obj/project.assets.json proves a restore ran here; Debug/Release proves a build did.
                return IsDotnetProject(fs, parent)
                    && fs.FileExists(Path.Combine(parent, "obj", "project.assets.json"))
                    && (HasConfiguration(fs, directory)
                        || fs.EnumerateDirectories(directory).Any(platform => HasConfiguration(fs, platform)));

            case "obj":
                return IsDotnetProject(fs, parent) && Inside("project.assets.json");

            case "target":
                return (Has("Cargo.toml") && Inside("CACHEDIR.TAG"))
                    || (Has("pom.xml") && (Inside("classes") || Inside("maven-status")));

            case "node_modules":
                return Has("package.json") && (JsLockfiles.Any(Has) || JsInstallStamps.Any(Inside));

            case "dist":
                return (Has("package.json") || Has("pyproject.toml") || Has("setup.py"))
                    && GitIgnore.Ignores(fs, parent, name);

            case "build":
                return BuildSystems.Any(Has)
                    && (Inside("CMakeCache.txt") || Inside("meson-info") || Inside("tmp") || GitIgnore.Ignores(fs, parent, name));

            case ".gradle":
                return Has("settings.gradle") || Has("settings.gradle.kts") || Has("build.gradle") || Has("build.gradle.kts");

            case ".venv":
            case "venv":
                return Inside("pyvenv.cfg")
                    && (PythonProjects.Any(Has) || fs.EnumerateFiles(parent, "requirements*.txt").Any());

            case "__pycache__":
                return true;

            case ".tox":
                return Has("tox.ini") || Has("pyproject.toml") || Has("setup.cfg");

            case ".nox":
                return Has("noxfile.py");

            case ".terragrunt-cache":
                return Has("terragrunt.hcl");
        }

        if (JsToolFolders.Contains(name))
        {
            return Has("package.json");
        }

        return TaggedCaches.Contains(name) && Inside("CACHEDIR.TAG");
    }

    /// <summary>A build configuration folder (bin/Debug, or bin/x64/Release one level down).</summary>
    private static bool HasConfiguration(IFileSystemService fs, string directory) =>
        fs.DirectoryExists(Path.Combine(directory, "Debug")) || fs.DirectoryExists(Path.Combine(directory, "Release"));

    private static bool IsDotnetProject(IFileSystemService fs, string directory) =>
        DotnetProjects.Any(pattern => fs.EnumerateFiles(directory, pattern).Any());
}
