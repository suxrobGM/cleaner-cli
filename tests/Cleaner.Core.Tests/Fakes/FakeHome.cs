namespace Cleaner.Core.Tests.Fakes;

/// <summary>
/// A home folder laid out like a real developer machine: Scoop, Cargo, rustup, nvm, Bun, WinGet,
/// <c>~/.local/bin</c>, editor extensions, a few installed apps, real caches, and real projects,
/// with a PATH that points into the installs. Modelled on the machine the 2026-10-24 run broke.
/// </summary>
public sealed class FakeHome
{
    private FakeHome(FakeFileSystem fileSystem, FakeEnvironment environment)
    {
        FileSystem = fileSystem;
        Environment = environment;
    }

    public FakeFileSystem FileSystem { get; }

    public FakeEnvironment Environment { get; }

    public string Home => Environment.HomeDirectory;

    /// <summary>Folder holding the projects whose build output may be swept.</summary>
    public string Repos { get; private init; } = string.Empty;

    /// <summary>Files of installed tools, apps, and source code. None may ever disappear.</summary>
    public List<string> Installed { get; } = [];

    /// <summary>Files a cache cleaner should remove when it runs.</summary>
    public List<string> Caches { get; } = [];

    /// <summary>Files inside project build output, removed only when the projects folder is swept.</summary>
    public List<string> ProjectArtifacts { get; } = [];

    public static FakeHome Windows()
    {
        var env = FakeEnvironment.Windows();
        env.IsElevated = true;
        var h = env.HomeDirectory;
        var l = env.LocalAppDataDirectory;
        var r = env.AppDataDirectory;
        var home = new FakeHome(new FakeFileSystem(), env) { Repos = $@"{h}\source\repos" };

        // Tools and apps that are installed, not cached.
        home.Install(
            $@"{h}\.bun\bin\bun.exe",
            $@"{h}\.cargo\bin\cargo.exe",
            $@"{h}\.cargo\registry\index\index.crates.io\config.json",
            $@"{h}\.rustup\toolchains\stable-x86_64-pc-windows-msvc\bin\rustc.exe",
            $@"{h}\scoop\apps\scoop\0.5\bin\scoop.ps1",
            $@"{h}\scoop\apps\scoop\0.5\supporting\validator\bin\validator.exe",
            $@"{h}\scoop\apps\scoop\0.5\.git\HEAD",
            $@"{h}\scoop\apps\make\4.4.1\bin\make.exe",
            $@"{h}\scoop\apps\openssl\3.5.0\bin\openssl.exe",
            $@"{h}\scoop\shims\make.exe",
            $@"{l}\Author Software\nvm\installs\v24.21.0\node.exe",
            $@"{l}\Author Software\nvm\installs\v24.21.0\node_modules\npm\package.json",
            $@"{l}\Microsoft\WinGet\Packages\Gyan.FFmpeg_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin\ffmpeg.exe",
            $@"{l}\Microsoft\WinGet\Packages\Gyan.FFmpeg_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\doc\ffmpeg.html",
            $@"{l}\Microsoft\WinGet\Packages\Gyan.FFmpeg_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\presets\libvpx.ffpreset",
            $@"{l}\Microsoft\WinGet\Packages\ggml.llamacpp_8wekyb3d8bbwe\llama-server.exe",
            $@"{h}\.local\bin\claude.exe",
            $@"{h}\bin\deploy.ps1",
            $@"{h}\.aspire\bin\aspire.exe",
            $@"{h}\.depvault\bin\depvault.exe",
            $@"{h}\.dotnet\tools\dotnet-ef.exe",
            $@"{l}\Unity\bin\unity.exe",
            $@"{l}\Python\bin\python.exe",
            $@"{l}\Programs\Ollama\ollama.exe",
            $@"{l}\Programs\Ollama\lib\ollama\ggml.dll",
            $@"{r}\npm\node_modules\typescript\package.json",
            $@"{r}\uv\tools\ruff\Scripts\ruff.exe",
            $@"{h}\.vscode\extensions\kennethguo.vscode-counter-3.8.1\package.json",
            $@"{h}\.vscode\extensions\kennethguo.vscode-counter-3.8.1\dist\extension.js",
            $@"{h}\.vscode\extensions\kennethguo.vscode-counter-3.8.1\node_modules\minimatch\index.js",
            $@"{h}\.vscode\extensions\esbenp.prettier-vscode-11.0.0\package.json",
            $@"{h}\.vscode\extensions\esbenp.prettier-vscode-11.0.0\package-lock.json",
            $@"{h}\.vscode\extensions\esbenp.prettier-vscode-11.0.0\dist\extension.js",
            $@"{h}\.vscode\extensions\esbenp.prettier-vscode-11.0.0\node_modules\prettier\index.js",
            @"C:\Program Files\Microsoft Visual Studio\18\Community\VC\vcpkg\vcpkg.exe",
            @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",
            $@"{h}\.ssh\id_ed25519",
            $@"{h}\Documents\notes.txt");

        // Junctions the old sweep followed into live installs.
        home.FileSystem.AddLink($@"{h}\scoop\apps\scoop\current", $@"{h}\scoop\apps\scoop\0.5");
        home.FileSystem.AddLink($@"{h}\scoop\apps\openssl\current", $@"{h}\scoop\apps\openssl\3.5.0");
        home.FileSystem.AddLink($@"{l}\Author Software\nvm\.nodejs", $@"{l}\Author Software\nvm\installs\v24.21.0");

        // Source code and project files in the workspace.
        home.Install(
            $@"{home.Repos}\app\.git\HEAD",
            $@"{home.Repos}\app\App.csproj",
            $@"{home.Repos}\app\Program.cs",
            $@"{home.Repos}\web\.git\HEAD",
            $@"{home.Repos}\web\package.json",
            $@"{home.Repos}\web\package-lock.json",
            $@"{home.Repos}\web\src\index.ts",
            $@"{home.Repos}\crate\Cargo.toml",
            $@"{home.Repos}\crate\src\main.rs",
            $@"{home.Repos}\tools\bin\committed-tool.exe");
        home.Artifact(
            $@"{home.Repos}\app\obj\project.assets.json",
            $@"{home.Repos}\app\bin\Debug\net10.0\App.dll",
            $@"{home.Repos}\web\node_modules\react\index.js",
            $@"{home.Repos}\crate\target\CACHEDIR.TAG",
            $@"{home.Repos}\crate\target\debug\crate.exe");

        // Real caches: each one is something its tool downloads again by itself.
        home.Cache(
            $@"{h}\.bun\install\cache\react@19.0.0\index.js",
            $@"{h}\.cargo\registry\cache\index.crates.io\serde-1.0.0.crate",
            $@"{h}\.rustup\downloads\rust-std.tar.xz",
            $@"{h}\scoop\cache\make#4.4.1#x.zip",
            $@"{l}\npm-cache\_cacache\content-v2\sha512\ab",
            $@"{l}\uv\cache\wheels-v1\x.whl",
            $@"{l}\pip\Cache\http-v2\a",
            $@"{l}\Unity\cache\packages\x.tgz",
            $@"{l}\Temp\junk\setup.log",
            @"C:\Windows\Temp\update.tmp",
            $@"{l}\NuGet\v3-cache\x.dat");

        env.PathDirectories =
        [
            $@"{h}\.aspire\bin", $@"{h}\scoop\apps\openssl\current\bin", $@"{l}\Author Software\nvm",
            $@"{l}\Author Software\nvm\.nodejs", $@"{h}\scoop\shims", $@"{h}\.depvault\bin", $@"{h}\.cargo\bin",
            $@"{l}\Microsoft\WindowsApps", $@"{h}\.local\bin", $@"{h}\.bun\bin", $@"{l}\Python\bin",
            @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64", $@"{h}\.dotnet\tools",
            @"C:\Program Files\Microsoft Visual Studio\18\Community\VC\vcpkg", $@"{l}\Programs\Ollama",
            $@"{l}\Microsoft\WinGet\Packages\ggml.llamacpp_8wekyb3d8bbwe", $@"{l}\Unity\bin",
            $@"{l}\Microsoft\WinGet\Packages\Gyan.FFmpeg_8wekyb3d8bbwe\ffmpeg-8.1.2-full_build\bin",
            @"C:\Windows\system32", @"C:\Windows", @"C:\Program Files\Git\cmd",
        ];

        return home;
    }

    public static FakeHome Linux()
    {
        var env = new FakeEnvironment();
        var h = env.HomeDirectory;
        var home = new FakeHome(new FakeFileSystem(), env) { Repos = $"{h}/src" };

        home.Install(
            $"{h}/.cargo/bin/cargo",
            $"{h}/.rustup/toolchains/stable-x86_64-unknown-linux-gnu/bin/rustc",
            $"{h}/.nvm/versions/node/v24.21.0/bin/node",
            $"{h}/.nvm/versions/node/v24.21.0/lib/node_modules/npm/package.json",
            $"{h}/.local/bin/claude",
            $"{h}/.local/share/uv/tools/ruff/bin/ruff",
            $"{h}/.bun/bin/bun",
            $"{h}/bin/backup.sh",
            $"{h}/.vscode/extensions/ms-python.python-2026.1.0/package.json",
            $"{h}/.vscode/extensions/ms-python.python-2026.1.0/dist/extension.js",
            $"{h}/.vscode/extensions/ms-python.python-2026.1.0/node_modules/vscode-jsonrpc/index.js",
            $"{h}/src/api/.git/HEAD",
            $"{h}/src/api/pyproject.toml",
            $"{h}/src/api/main.py");
        home.Artifact(
            $"{h}/src/api/.venv/pyvenv.cfg",
            $"{h}/src/api/.venv/lib/python3.13/site-packages/x.py");
        home.Cache(
            $"{h}/.cache/pip/http-v2/a",
            $"{h}/.cache/uv/wheels-v1/x.whl",
            $"{h}/.bun/install/cache/react@19.0.0/index.js",
            $"{h}/.cargo/registry/cache/index.crates.io/serde-1.0.0.crate");

        env.PathDirectories =
        [
            $"{h}/.cargo/bin", $"{h}/.local/bin", $"{h}/.bun/bin", $"{h}/bin",
            $"{h}/.nvm/versions/node/v24.21.0/bin", "/usr/local/bin", "/usr/bin", "/bin",
        ];
        env.SetVariable("NVM_DIR", $"{h}/.nvm");

        return home;
    }

    /// <summary>
    /// The commands on PATH: files directly inside each PATH folder, to compare before and after a
    /// run. Deeper files of an install are in <see cref="Installed"/>; recursing here would count
    /// C:\Windows\Temp as "on PATH" because C:\Windows is.
    /// </summary>
    public HashSet<string> FilesOnPath() =>
        Environment.PathDirectories
            .SelectMany(dir => FileSystem.EnumerateFiles(dir))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private void Install(params string[] files) => Add(Installed, files);

    private void Artifact(params string[] files) => Add(ProjectArtifacts, files);

    private void Cache(params string[] files) => Add(Caches, files);

    private void Add(List<string> list, string[] files)
    {
        foreach (var file in files)
        {
            FileSystem.AddFile(file, 100);
            list.Add(file);
        }
    }
}
