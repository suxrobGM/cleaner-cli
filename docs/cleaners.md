# Cleaners

Cleaner ships with 120 cleaners. Run `cleaner list` to see which apply to your machine. Use the
**id** with `cleaner clean <id>` / `cleaner scan <id>`.

> Cleaners only ever remove caches, temp files, and rebuildable artifacts — never source, configs,
> credentials, installed games, or save data.

Workspace-sweeping cleaners (`build-artifacts`, `unity`) take the `--path`/`-p <dir>` option, which
can be repeated to scan several workspaces at once, e.g.
`cleaner clean build-artifacts unity -p ~/source -p ~/work`. It defaults to the current directory.

Cleaners honor the usual cache-relocation environment variables (`NUGET_PACKAGES`, `CARGO_HOME`,
`GOMODCACHE`, `GRADLE_USER_HOME`, `npm_config_cache`, `YARN_CACHE_FOLDER`, `PIP_CACHE_DIR`,
`UV_CACHE_DIR`, `CONAN_HOME`, `PUB_CACHE`, and friends) — a relocated cache is scanned and cleaned
where the tool actually keeps it.

Command-based cleaners (`docker`, `podman`, `winsxs`, `dnf`, …) can't estimate their size up front;
`scan` shows them as *n/a (runs command)* and reports the space after they run. Cleaners marked
**needs --force** have a real trade-off beyond "cache is re-downloaded" and are skipped by `clean`
(with a message) unless `--force` is given.

## Package managers (.NET)

| Id | Removes |
| --- | --- |
| `nuget` | NuGet caches (`dotnet nuget locals all --clear`; global-packages, http/plugins/temp). |
| `dotnet` | .NET SDK template-engine and telemetry caches. |

## JavaScript / TypeScript

| Id | Removes |
| --- | --- |
| `npm` | npm cache (`npm cache clean --force`). |
| `npx` | npx execution cache (`_npx`). |
| `yarn` | Yarn cache (`yarn cache clean`). |
| `pnpm` | pnpm store (`pnpm store prune`). |
| `bun` | Bun install cache (covers bunx). |
| `deno` | Deno module/dependency cache (`DENO_DIR`). |

## Python

| Id | Removes |
| --- | --- |
| `pip` | pip cache (`pip cache purge`). |
| `pipenv` | pipenv cache. |
| `poetry` | Poetry artifact/download cache. |
| `conda` | Conda package cache (`conda clean --all`). |
| `pdm` | PDM cache (`pdm cache clear`). |
| `uv` | uv download/build cache. |
| `pipx` | pipx cache and logs. Never touches installed venvs. |
| `pre-commit` | pre-commit hook environment cache (rebuilt on the next run). |

## Rust

| Id | Removes |
| --- | --- |
| `cargo` | Cargo registry cache, extracted sources, git sources (keeps `~/.cargo/bin`). |
| `rustup` | rustup download/temp directories. |
| `sccache` | sccache compilation cache. |

## Go

| Id | Removes |
| --- | --- |
| `go` | Module and build caches (`go clean -cache -modcache`). |

## Machine learning

| Id | Removes |
| --- | --- |
| `ml-cache` | HuggingFace hub/datasets, Torch hub, Keras datasets, and kagglehub caches (honors `HF_HOME`/`TRANSFORMERS_CACHE`/`TORCH_HOME`/`KERAS_HOME`). Leaves installed model registries like `~/.ollama/models` alone. |
| `wandb` | Weights & Biases artifact/download cache. Run data under `~/wandb` is never touched. |

## JVM / Android

| Id | Removes |
| --- | --- |
| `gradle` | Gradle build caches, wrapper distributions (`wrapper/dists`), and daemon logs. |
| `maven` | Maven local repository and wrapper distributions. |
| `sbt` | sbt / Ivy resolution caches. |
| `konan` | Kotlin/Native compiler caches, toolchain dependencies, and auto-downloaded compiler distributions (`~/.konan`). |
| `android` | Android SDK and build caches (keeps installed SDKs and AVDs). |

## Mobile (React Native / Expo)

| Id | Removes |
| --- | --- |
| `react-native` | Metro/Haste temp caches. |
| `expo` | Global `~/.expo` and project `.expo` caches. |
| `cocoapods` | CocoaPods spec-repo and pod download cache (macOS). |

## Other languages

| Id | Removes |
| --- | --- |
| `bundler` | Ruby Bundler cache. |
| `composer` | PHP Composer cache. |
| `pub` | Dart/Flutter pub cache (hosted, git, temp). |
| `hex` | Elixir Hex packages and Mix archives. |
| `vcpkg` | vcpkg download and binary-archive caches (C/C++). |
| `haskell` | cabal packages and stack pantry. |
| `conan` | Conan (C/C++) source/build/download/temp folders (`conan cache clean "*"`). With `--force` also removes cached packages (`conan remove "*"`); they re-download or rebuild. Honors `CONAN_HOME`. |
| `zig` | Zig global compilation cache. |
| `swiftpm` | Swift Package Manager repository/artifact caches (macOS/Linux). |
| `opam` | opam (OCaml) download cache. Keeps switches and installed packages. |
| `cpanm` | cpanm (Perl) build work directories. |
| `julia` | Julia precompiled code and logs. Never touches `packages`, `artifacts`, or `scratchspaces`. |
| `rubygems` | Superseded gem versions (`gem cleanup`) and the spec index cache. |
| `renv` | renv (R) global package cache (honors `RENV_PATHS_CACHE`). |
| `luarocks` | LuaRocks download/build cache. |
| `nim` | Nim compiler cache (nimcache). Keeps installed nimble packages. |
| `texlive` | TeX Live luatex/font caches under `~/.texlive*/texmf-var` (regenerated on use). |

## Build / monorepo caches

| Id | Removes |
| --- | --- |
| `ccache` | ccache compiler cache. |
| `bazel` | Bazel disk/repository cache. |
| `turbo-nx` | Turborepo and Nx caches (global + project-local). |
| `node-modules-cache` | `node_modules/.cache` under the working directory. |

## Containers / IaC

| Id | Removes |
| --- | --- |
| `docker` | Dangling images, stopped containers, unused networks, and **all** unused build cache (`docker system prune` + `docker builder prune -a`). With `--force` also removes every unused image and **named volume** (`docker system prune -a --volumes`) — that can delete data such as database volumes. On Docker Desktop/WSL2 this frees space inside the virtual disk; compact the `.vhdx` separately to shrink the host file. |
| `terraform` | Terraform provider plugin cache. |
| `podman` | `podman system prune` (with `--force`: `-a --volumes`, same caveats as docker). |
| `helm` | Helm chart repository cache (honors `HELM_REPOSITORY_CACHE`). |
| `minikube` | minikube image/ISO download cache. Keeps profiles and VMs. |
| `vagrant` | Vagrant temp downloads (`~/.vagrant.d/tmp`). **Boxes are never touched.** |
| `pulumi` | Pulumi provider plugin binaries (re-downloaded on demand). |
| `kubectl` | kubectl discovery and HTTP caches. kubeconfig/credentials untouched. |
| `ansible` | Ansible temp workspace and Galaxy download cache. |
| `lima` | Lima/colima VM image download cache (macOS/Linux). |

## IDEs / editors

| Id | Removes |
| --- | --- |
| `jetbrains` | JetBrains IDE caches/logs. On Windows only the per-product `caches`/`index`/`log`/`tmp` dirs — Toolbox-installed IDEs and LocalHistory are never touched. |
| `vscode` | VS Code cache directories, plus the same for Cursor, VSCodium, and Windsurf (keeps settings and extensions). |
| `visualstudio` | Project `.vs` and ComponentModelCache (Windows). |
| `xcode` | Xcode DerivedData, iOS/watchOS DeviceSupport symbol caches, and simulator caches (macOS). Archives are never touched. |
| `zed` | Zed editor cache. |
| `neovim` | Neovim cache directory (treesitter/luac artifacts, logs). Keeps shada and sessions. |

## Tooling downloads

| Id | Removes |
| --- | --- |
| `browser-automation` | Playwright, Puppeteer, and Cypress browser downloads (incl. the `~/.cache` locations they use even on Windows/macOS). |
| `electron` | Electron and electron-builder download caches. |
| `azure-functions` | Azure Functions Core Tools downloaded runtime feeds. |
| `dotslash` | DotSlash fetched-executable cache. |
| `corepack` | Corepack's downloaded package-manager binaries. |
| `nvm` | nvm download cache (POSIX; installed Node versions are kept). |
| `mise` | mise download/HTTP cache. Installed tools are kept. |
| `asdf` | asdf downloads and temp. Installed tools are kept. |
| `sdkman` | SDKMAN! archives and temp. Installed candidates are kept. |
| `node-gyp` | node-gyp downloaded Node headers/import libraries. |
| `gcloud` | Google Cloud CLI logs and surface caches. Config/credentials untouched. |
| `sonar` | SonarLint / sonar-scanner plugin and analyzer cache. |

## Project-local

| Id | Removes |
| --- | --- |
| `build-artifacts` | `bin`, `obj`, `node_modules`, `target`, `dist`, `.next`, `.nuxt`, `.svelte-kit`, `.astro`, `.gradle`, `__pycache__`, `.pytest_cache`, `.mypy_cache`, `.ruff_cache`, `.terragrunt-cache` under each `--path` (default cwd). Repeat `-p` to sweep whole workspaces. |

## Operating system

| Id | Removes | Notes |
| --- | --- | --- |
| `temp` | The per-user temp directory. | |
| `trash` | Recycle Bin / Trash for the current user. | |
| `browser-cache` | Chrome, Edge, Brave, Opera, Vivaldi, Chromium, Arc, and Firefox HTTP/code caches. | Keeps history/cookies/profiles. |
| `windows-update` | `SoftwareDistribution\Download`. | Windows · needs admin |
| `windows-temp` | The machine-wide `Windows\Temp`. | Windows · needs admin |
| `windows-logs` | `Windows\Logs` (CBS, DISM, WindowsUpdate servicing logs). | Windows · needs admin |
| `service-temp` | Temp dirs of the `LocalService` / `NetworkService` accounts. | Windows · needs admin |
| `downloaded-program-files` | `Windows\Downloaded Program Files` and `Downloaded Installations`. | Windows · needs admin |
| `memory-dumps` | Kernel crash dumps (`Windows\Minidump`, `LiveKernelReports`). | Windows · needs admin |
| `gpu-shader-cache` | GPU shader caches (`D3DSCache`, NVIDIA `DXCache`/`GLCache`/`NV_Cache`, AMD, Intel). | Windows |
| `inet-cache` | WinINet "Temporary Internet Files" (`...\Windows\INetCache`). | Windows |
| `store-app-cache` | Microsoft Store / UWP per-package caches (`AC\INetCache`, `AC\Temp`, `TempState`). | Windows |
| `thumbnails` | Explorer thumbnail/icon cache. | Windows |
| `crash-dumps` | Crash dumps and Windows Error Reporting queues. | Windows |
| `delivery-optimization` | Delivery Optimization download cache. | Windows · needs admin |
| `gpu-installers` | GPU driver installer leftovers: `C:\NVIDIA`, `C:\AMD`, `C:\Intel` extraction folders and NVIDIA's download cache. Never touches DriverStore, `Installer2`, or installed drivers. | Windows · needs admin |
| `winsxs` | Superseded Windows component-store versions (`DISM /StartComponentCleanup`; no `/ResetBase`, so updates stay uninstallable). Slow (minutes) but often the largest Windows reclaim. | Windows · needs admin |
| `windows-old` | The previous Windows installation (`C:\Windows.old`). Deleting it removes the ability to roll back the last upgrade. | Windows · needs admin · **needs --force** |
| `mac-caches` | `~/Library/Caches` and `~/Library/Logs`. | macOS |
| `xdg-cache` | The `~/.cache` user cache root. | Linux |
| `journal` | Vacuums the systemd journal to 100 MB. | Linux · needs admin |

## System package managers

| Id | Removes | Notes |
| --- | --- | --- |
| `apt` | APT archive cache (`apt-get clean`). | Linux · needs admin |
| `dnf` | DNF cache (`dnf clean all`). | Linux · needs admin |
| `pacman` | Pacman cache (`pacman -Sc`). | Linux · needs admin |
| `brew` | Homebrew cache (`brew cleanup -s`). | macOS/Linux |
| `scoop` | Scoop download cache (`scoop cache rm *`). | Windows |
| `choco` | Chocolatey cache (`choco cache remove`). | Windows · needs admin |
| `winget` | winget installer downloads and diagnostic logs. Source indexes are left alone. | Windows |
| `flatpak` | Unused Flatpak runtimes (`flatpak uninstall --unused`). | Linux |
| `nix` | Unreachable Nix store paths (`nix-collect-garbage`; keeps rollback generations). | macOS/Linux |

## Game development

| Id | Removes |
| --- | --- |
| `unity` | Unity global editor cache plus regenerable per-project `Library`/`Temp`/`Logs`/`obj` for Unity projects found under each `--path`. Detection-gated (needs `Assets` + `ProjectSettings`); keeps `Assets`, player builds, and the Asset Store cache. |
| `unreal` | Unreal Engine's shared DerivedDataCache (shaders/asset derivations; rebuilt on demand). Projects and engine installs are never touched. |

## Applications

| Id | Removes |
| --- | --- |
| `steam` | Steam shader/download/web caches, logs, dumps. **Never** touches installed games or saves. |
| `electron-app-cache` | Chromium HTTP/GPU/shader caches of common Electron apps (Discord, Slack, Teams — classic and the new WebView2 client — Claude, MongoDB Compass, Postman, Notion, Obsidian, Figma, Signal, GitHub Desktop, WhatsApp, Element). Keeps config, local storage, and login state. |
| `spotify` | Spotify offline media and data caches. Keeps settings and login state. |
| `telegram` | Telegram Desktop media/emoji caches — the same data its own "Clear cache" button removes. Account state under `tdata` is never touched, so it can't log you out. |
| `game-launchers` | Web/browser caches of Epic Games, Battle.net, GOG Galaxy, the EA app, and the Riot Client. Game installs, saves, and login state are never touched. |
| `adobe-media-cache` | Adobe shared media caches (Premiere/After Effects render, database, and audio peak files; regenerated). |
| `onedrive` | OneDrive client and setup logs (Windows). Synced content is never touched. |
| `dropbox` | Dropbox's internal `.dropbox.cache` staging folder (officially safe to purge). Synced files are never touched. |
