# Cleaners

Cleaner has 132 cleaners, listed here in menu order. **List all cleaners** shows which apply to
your machine.

The **Notes** column shows:

- the platform, when not all three;
- **admin**: needs administrator or root;
- **asks again**: has a real trade-off, so it asks for its own confirmation;
- what is kept, where it matters.

Cleaners remove caches, temp files, and rebuildable output only. Every path also passes the
[deletion guard](usage.md#what-cleaner-never-deletes).

## How sizes and commands work

- Cleaners follow cache-relocation variables such as `NUGET_PACKAGES`, `CARGO_HOME`, `GOMODCACHE`,
  `GRADLE_USER_HOME`, `npm_config_cache`, `YARN_CACHE_FOLDER`, `PIP_CACHE_DIR`, `UV_CACHE_DIR`, and
  `PUB_CACHE`.
- A command in the table (for example `npm cache clean --force`) runs when the tool is on PATH. If it
  fails, nothing is deleted instead. If the tool is not installed, its cache folders are deleted
  directly.
- `docker` and `docker-vhdx` ask `docker system df` for their size. `winsxs` is measured only around
  the cleanup, because DISM's size report takes minutes. When a size is unknown, the preview shows
  *n/a* and the summary reports it after the run.

## Operating system

### System caches & temp

| Id | Removes | Notes |
| --- | --- | --- |
| `temp` | Per-user temp folder. | |
| `trash` | Recycle Bin / Trash. | |
| `unfinished-deletes` | Folders an earlier run renamed aside but could not delete (from `~/.cleaner/pending-deletes.txt`). | |
| `browser-cache` | HTTP/code caches of Chrome, Edge, Brave, Opera, Vivaldi, Chromium, Arc, and Firefox, plus Chrome/Edge on-device AI models. | Keeps history, cookies, profiles. |
| `windows-update` | `SoftwareDistribution\Download`. | Windows, admin |
| `windows-temp` | `Windows\Temp`. | Windows, admin |
| `thumbnails` | Explorer thumbnail and icon caches. | Windows |
| `crash-dumps` | App crash dumps and Windows Error Reporting queues. | Windows |
| `delivery-optimization` | Delivery Optimization cache. | Windows, admin |
| `windows-logs` | `Windows\Logs` (CBS, DISM, Windows Update). | Windows, admin |
| `service-temp` | Temp folders of LocalService and NetworkService. | Windows, admin |
| `downloaded-program-files` | `Downloaded Program Files` and `Downloaded Installations`. | Windows, admin |
| `memory-dumps` | Kernel dumps (`Minidump`, `LiveKernelReports`). | Windows, admin |
| `gpu-shader-cache` | DirectX, NVIDIA, AMD, and Intel shader caches. | Windows |
| `inet-cache` | WinINet `INetCache`. | Windows |
| `store-app-cache` | Microsoft Store/UWP app caches and temp state. | Windows |
| `mac-caches` | `~/Library/Caches` and `~/Library/Logs`. | macOS |
| `xdg-cache` | `~/.cache`. | Linux |
| `journal` | Vacuums the systemd journal to 100 MB. | Linux, admin |
| `gpu-installers` | `C:\NVIDIA`, `C:\AMD`, `C:\Intel` extraction folders, NVIDIA downloads, NVIDIA app update staging and logs, NGX (DLSS) models. | Windows, admin. Keeps DriverStore and installed drivers. |
| `amd-telemetry` | AMD usage logs under `ProgramData\AMD\PPC`. | Windows. Keeps `config.csv`. |
| `winre-agent` | `C:\$WinREAgent`, left over from feature updates. | Windows, admin |
| `ngen-cache` | .NET Framework native images (`NativeImages_v*`). | Windows, admin, **asks again**: Framework apps start slower until rebuilt. |
| `windows-installer-orphans` | `.msi`/`.msp` files in `C:\Windows\Installer` that nothing installed references. Does nothing if the registry read fails. | Windows, admin, **asks again** |
| `winsxs` | Superseded component versions (`DISM /StartComponentCleanup`, no `/ResetBase`). | Windows, admin, **asks again**: runs for many minutes with no progress. |
| `windows-old` | `C:\Windows.old`. | Windows, admin, **asks again**: removes rollback to the previous Windows. |

### System package managers

| Id | Removes | Notes |
| --- | --- | --- |
| `apt` | APT archives (`apt-get clean`). | Linux, admin |
| `dnf` | DNF cache (`dnf clean all`). | Linux, admin |
| `pacman` | Pacman cache (`pacman -Sc`). | Linux, admin |
| `brew` | Homebrew cache (`brew cleanup -s`). | macOS, Linux |
| `scoop` | Scoop cache (`scoop cache rm *`), then old app versions (`scoop cleanup *`). | Windows, **asks again**: `scoop reset app@version` can no longer roll back. |
| `choco` | Chocolatey cache (`choco cache remove`). | Windows, admin |
| `flatpak` | Unused runtimes (`flatpak uninstall --unused`). | Linux |
| `nix` | Unreachable store paths (`nix-collect-garbage`). | macOS, Linux. Keeps rollback generations. |
| `winget` | Installer downloads and diagnostic logs. | Windows. Keeps source indexes. |

## Development tools

### .NET

| Id | Removes | Notes |
| --- | --- | --- |
| `nuget` | NuGet caches (`dotnet nuget locals all --clear`). | |
| `dotnet` | .NET SDK telemetry cache. | Keeps `dotnet new` templates. |

### JavaScript / TypeScript

| Id | Removes | Notes |
| --- | --- | --- |
| `npm` | npm cache (`npm cache clean --force`). | |
| `npx` | npx cache (`_npx`). | |
| `yarn` | Yarn cache (`yarn cache clean`). | |
| `pnpm` | Unreferenced store packages (`pnpm store prune`). | |
| `bun` | Bun install cache (`bun pm cache rm`). | |
| `deno` | Deno module cache. | |

### Python

| Id | Removes | Notes |
| --- | --- | --- |
| `pip` | pip cache (`pip cache purge`). | |
| `pipenv` | pipenv cache. | |
| `poetry` | Poetry `cache` and `artifacts`. | Keeps virtualenvs. |
| `conda` | Package cache (`conda clean --all`). Without conda, only downloaded archives. | |
| `pdm` | PDM cache (`pdm cache clear`). | |
| `uv` | uv cache (`uv cache clean`). | |
| `pipx` | pipx cache and logs. | Keeps installed apps. |
| `pre-commit` | Hook environments (`pre-commit clean`). | |

### JVM / Android

| Id | Removes | Notes |
| --- | --- | --- |
| `gradle` | Gradle `caches`, `wrapper/dists`, and daemon logs. | |
| `maven` | `~/.m2/repository` and wrapper distributions. | **asks again**: artifacts from `mvn install` must be rebuilt. |
| `sbt` | sbt and Ivy caches. | |
| `konan` | Kotlin/Native caches and downloaded compilers. | |
| `android` | Android SDK and build caches. | Keeps SDKs and AVDs. |

### Rust

| Id | Removes | Notes |
| --- | --- | --- |
| `cargo` | Registry cache, extracted sources, git sources. | Keeps `~/.cargo/bin`. |
| `rustup` | rustup `downloads` and `tmp`. | |
| `sccache` | sccache cache. | |

### Go

| Id | Removes | Notes |
| --- | --- | --- |
| `go` | Build and module caches (`go clean -cache -modcache`). | |

### Other languages

| Id | Removes | Notes |
| --- | --- | --- |
| `bundler` | Bundler cache. | |
| `composer` | Composer cache. | |
| `pub` | Dart/Flutter pub cache. | |
| `hex` | Hex package cache. | Keeps Mix archives. |
| `vcpkg` | vcpkg download and binary caches. | |
| `haskell` | cabal packages and stack pantry. | |
| `conan` | Conan source/build/download/temp folders (`conan cache clean "*"`). Needs `conan` on PATH. | Keeps package binaries. |
| `zig` | Zig global cache. | |
| `swiftpm` | SwiftPM repository and artifact caches. | macOS, Linux |
| `opam` | opam download cache. | macOS, Linux. Keeps switches. |
| `cpanm` | cpanm build folders. | macOS, Linux |
| `julia` | Precompiled code and logs. | Keeps packages and artifacts. |
| `rubygems` | RubyGems spec cache. | Keeps installed gems. |
| `renv` | renv global package cache. | |
| `luarocks` | LuaRocks cache. | |
| `nim` | nimcache. | Keeps nimble packages. |
| `texlive` | TeX Live `texmf-var` caches. | macOS, Linux |

### Machine learning

| Id | Removes | Notes |
| --- | --- | --- |
| `ml-cache` | Hugging Face `hub`/`xet`/`datasets`, Torch hub, Keras datasets, kagglehub. | Keeps the Hugging Face login and Ollama models. |
| `wandb` | Weights & Biases artifact cache. | Keeps run data. |

### Game development

| Id | Removes | Notes |
| --- | --- | --- |
| `unity` | Unity editor cache, and `Library`/`Temp`/`Logs`/`obj` in Unity projects under each `--path`. | Keeps `Assets` and builds. |
| `unreal` | Unreal shared DerivedDataCache. | |

### Mobile (React Native / Expo)

| Id | Removes | Notes |
| --- | --- | --- |
| `react-native` | Metro/Haste temp caches. | |
| `expo` | Project `.expo` cache. | Keeps the Expo login. |
| `cocoapods` | CocoaPods spec repos and download cache. | macOS |

### Build / monorepo caches

| Id | Removes | Notes |
| --- | --- | --- |
| `ccache` | ccache cache. | |
| `bazel` | Bazel disk and repository caches. | |
| `turbo-nx` | Turborepo and Nx caches (global and project). | |
| `node-modules-cache` | `node_modules/.cache` in the project folder. | |

### Containers / IaC

| Id | Removes | Notes |
| --- | --- | --- |
| `docker` | Stopped containers, unused networks and images, build cache (`docker system prune -a`, `docker builder prune -a`). | Keeps volumes. |
| `docker-vhdx` | Compacts Docker Desktop's WSL2 disks (shuts down WSL, runs `diskpart`). Deletes nothing. | Windows, admin, **asks again** |
| `terraform` | Provider plugin cache. | |
| `podman` | `podman system prune -a`. | Keeps volumes. |
| `helm` | Chart repository cache. | |
| `minikube` | Image and ISO downloads. | Keeps profiles and VMs. |
| `vagrant` | `~/.vagrant.d/tmp`. | Keeps boxes. |
| `pulumi` | Provider plugins. | |
| `kubectl` | Discovery and HTTP caches. | |
| `ansible` | Temp workspace and Galaxy cache. | macOS, Linux |
| `lima` | Lima/colima image cache. | macOS, Linux |

### IDEs / editors

| Id | Removes | Notes |
| --- | --- | --- |
| `jetbrains` | JetBrains caches, indexes, logs, temp. | Keeps IDEs and LocalHistory. |
| `vscode` | VS Code, Cursor, VSCodium, and Windsurf caches. | Keeps settings and extensions. |
| `vscode-cpptools` | C/C++ extension IntelliSense caches (`ipch`, symbol databases). | |
| `android-studio` | Android Studio caches, indexes, logs, temp. | Keeps plugins and settings. |
| `visualstudio` | Project `.vs` and ComponentModelCache. | Windows |
| `xcode` | DerivedData, DeviceSupport, simulator caches. | macOS. Keeps archives. |
| `zed` | Zed cache. | |
| `neovim` | Neovim cache folder. | Keeps shada and sessions. |

### Tooling downloads

| Id | Removes | Notes |
| --- | --- | --- |
| `browser-automation` | Playwright, Puppeteer, and Cypress browsers. | **asks again**: tests need them reinstalled. Keeps Playwright MCP profiles. |
| `electron` | Electron and electron-builder downloads. | |
| `azure-functions` | Azure Functions Core Tools runtime feeds. | |
| `dotslash` | DotSlash cache. | |
| `corepack` | Corepack package-manager downloads. | |
| `nvm` | nvm download cache. | macOS, Linux. Keeps Node versions. |
| `mise` | mise cache (`mise cache clear`). | Keeps installed tools. |
| `asdf` | asdf downloads and temp. | macOS, Linux. Keeps installed tools. |
| `sdkman` | SDKMAN archives and temp. | macOS, Linux. Keeps installed SDKs. |
| `node-gyp` | Downloaded Node headers. | |
| `gcloud` | Google Cloud CLI logs and caches. | |
| `sonar` | SonarLint/sonar-scanner cache. | |
| `codex` | Codex CLI `cache`, temp, and rotated sandbox logs. | Keeps sessions, auth, and config. |

### Project-local

| Id | Removes | Notes |
| --- | --- | --- |
| `build-artifacts` | Build output and dependencies inside projects under each `--path`. | Opt-in, **asks again**: projects need a rebuild or reinstall. |

`build-artifacts` is outside **All cleaners**. It refuses to start from your home folder, a drive
root, or a system or install folder. It never follows links or enters dot-folders, `node_modules`,
or installed tools. A folder is taken only when both columns below match:

| Folder | Parent must contain | Proof inside or beside the folder |
| --- | --- | --- |
| `bin` | `*.csproj` / `*.fsproj` / `*.vbproj` | `obj/project.assets.json` beside it, and `Debug` or `Release` inside (directly or one level down) |
| `obj` | `*.csproj` / `*.fsproj` / `*.vbproj` | `project.assets.json` |
| `target` | `Cargo.toml` / `pom.xml` | `CACHEDIR.TAG` (Cargo) / `classes` or `maven-status` (Maven) |
| `node_modules` | `package.json` | a lockfile beside it, or `.package-lock.json`, `.modules.yaml`, `.yarn-state.yml`, `.yarn-integrity` |
| `dist` | `package.json` / `pyproject.toml` / `setup.py` | listed in a `.gitignore` up to the repository root |
| `build` | a Gradle, Maven, CMake, or Meson file | `CMakeCache.txt`, `meson-info`, or `tmp`, or listed in a `.gitignore` |
| `.gradle` | a Gradle settings or build file | none |
| `.next`, `.nuxt`, `.svelte-kit`, `.astro`, `.turbo`, `.parcel-cache`, `.vite`, `.angular` | `package.json` | none |
| `.venv`, `venv` | a Python project file or `requirements*.txt` | `pyvenv.cfg` |
| `.pytest_cache`, `.mypy_cache`, `.ruff_cache` | anything | `CACHEDIR.TAG` |
| `__pycache__` | anything | none |
| `.tox` | `tox.ini` / `pyproject.toml` / `setup.cfg` | none |
| `.nox` | `noxfile.py` | none |
| `.terragrunt-cache` | `terragrunt.hcl` | none |

## Applications

### Desktop apps

| Id | Removes | Notes |
| --- | --- | --- |
| `steam` | Steam shader, download, and web caches, logs, dumps. | Keeps games and saves. |
| `electron-app-cache` | Chromium caches of Discord, Slack, Teams, Claude, Postman, Notion, Obsidian, Figma, Signal, WhatsApp, and other Electron apps. | Keeps settings and logins. |
| `spotify` | Spotify offline media and data caches. | Keeps settings and login. |
| `telegram` | Telegram Desktop media and emoji caches. | Keeps `tdata` (account state). |
| `game-launchers` | Web caches of Epic Games, Battle.net, GOG Galaxy, EA app, and Riot Client. | Keeps games, saves, and logins. |
| `adobe-media-cache` | Adobe media cache, database, and peak files. | |
| `onedrive` | OneDrive logs. | Windows. Keeps synced files. |
| `dropbox` | `~/Dropbox/.dropbox.cache`. | Keeps synced files. |
| `razer` | Razer Cortex caches, logs, updates, and FPS history. | Windows. Keeps Synapse profiles. |
| `claude-desktop` | Claude Desktop agent VM images (`vm_bundles`, `claude-code-vm`). | Keeps chats and settings. |
| `app-leftovers` | Profiles of uninstalled Claude Desktop, Docker Desktop, Discord, Slack, Unity Hub, and Epic Games Launcher. Offered only when no install folder, MSIX package, PATH entry, or uninstall record remains. | Windows, macOS, **asks again**: this is settings and history, not cache. |
