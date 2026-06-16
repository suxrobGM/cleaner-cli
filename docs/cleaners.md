# Cleaners

Cleaner ships with 70+ cleaners. Run `cleaner list` to see which apply to your machine. Use the
**id** with `cleaner clean <id>` / `cleaner scan <id>`.

> Cleaners only ever remove caches, temp files, and rebuildable artifacts — never source, configs,
> credentials, installed games, or save data.

Workspace-sweeping cleaners (`build-artifacts`, `unity`) take the `--path`/`-p <dir>` option, which
can be repeated to scan several workspaces at once, e.g.
`cleaner clean build-artifacts unity -p ~/source -p ~/work`. It defaults to the current directory.

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
| `ml-cache` | HuggingFace hub/datasets and Torch hub caches under `~/.cache` (honors `HF_HOME`/`TRANSFORMERS_CACHE`/`TORCH_HOME`). Leaves installed model registries like `~/.ollama/models` alone. |

## JVM / Android

| Id | Removes |
| --- | --- |
| `gradle` | Gradle caches. |
| `maven` | Maven local repository. |
| `sbt` | sbt / Ivy resolution caches. |
| `konan` | Kotlin/Native compiler caches, toolchain dependencies, and auto-downloaded compiler distributions (`~/.konan`). |
| `android` | Android SDK and build caches (keeps installed SDKs and AVDs). |

## Mobile (React Native / Expo)

| Id | Removes |
| --- | --- |
| `react-native` | Metro/Haste temp caches and the CocoaPods cache (macOS). |
| `expo` | Global `~/.expo` and project `.expo` caches. |

## Other languages

| Id | Removes |
| --- | --- |
| `bundler` | Ruby Bundler cache. |
| `composer` | PHP Composer cache. |
| `pub` | Dart/Flutter pub cache (hosted, git, temp). |
| `hex` | Elixir Hex packages and Mix archives. |
| `vcpkg` | vcpkg download and binary-archive caches (C/C++). |
| `haskell` | cabal packages and stack pantry. |

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

## IDEs / editors

| Id | Removes |
| --- | --- |
| `jetbrains` | JetBrains IDE caches/logs. |
| `vscode` | VS Code cache directories (keeps settings and extensions). |
| `visualstudio` | Project `.vs` and ComponentModelCache (Windows). |
| `xcode` | Xcode DerivedData (macOS). |

## Tooling downloads

| Id | Removes |
| --- | --- |
| `browser-automation` | Playwright, Puppeteer, and Cypress browser downloads (incl. the `~/.cache` locations they use even on Windows/macOS). |
| `electron` | Electron and electron-builder download caches. |
| `azure-functions` | Azure Functions Core Tools downloaded runtime feeds. |
| `dotslash` | DotSlash fetched-executable cache. |

## Project-local

| Id | Removes |
| --- | --- |
| `build-artifacts` | `bin`, `obj`, `node_modules`, `target`, `dist`, `.next`, `.gradle` under each `--path` (default cwd). Repeat `-p` to sweep whole workspaces. |

## Operating system

| Id | Removes | Notes |
| --- | --- | --- |
| `temp` | The per-user temp directory. | |
| `trash` | Recycle Bin / Trash for the current user. | |
| `browser-cache` | Chrome/Edge/Firefox HTTP and code caches. | Keeps history/cookies/profiles. |
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

## Game development

| Id | Removes |
| --- | --- |
| `unity` | Unity global editor cache plus regenerable per-project `Library`/`Temp`/`Logs`/`obj` for Unity projects found under each `--path`. Detection-gated (needs `Assets` + `ProjectSettings`); keeps `Assets`, player builds, and the Asset Store cache. |

## Applications

| Id | Removes |
| --- | --- |
| `steam` | Steam shader/download/web caches, logs, dumps. **Never** touches installed games or saves. |
| `electron-app-cache` | Chromium HTTP/GPU/shader caches of common Electron apps (Discord, Slack, Teams, Claude, MongoDB Compass, Postman, Notion, Obsidian, Figma, Signal, GitHub Desktop). Keeps config and local storage. |
| `spotify` | Spotify offline media and data caches. Keeps settings and login state. |
