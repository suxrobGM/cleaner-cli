using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.DevTools;
using Microsoft.Extensions.DependencyInjection;

namespace Cleaner.Cli.Infrastructure;

internal static partial class ServiceCollectionExtensions
{
    /// <summary>Cleaners for developer tooling: languages, package managers, build caches, IDEs, etc.</summary>
    private static void AddDevToolCleaners(this IServiceCollection services)
    {
        // .NET
        services.AddSingleton<ICleaner, NuGetCleaner>();
        services.AddSingleton<ICleaner, DotnetCleaner>();

        // JavaScript / TypeScript
        services.AddSingleton<ICleaner, NpmCleaner>();
        services.AddSingleton<ICleaner, NpxCleaner>();
        services.AddSingleton<ICleaner, YarnCleaner>();
        services.AddSingleton<ICleaner, PnpmCleaner>();
        services.AddSingleton<ICleaner, BunCleaner>();
        services.AddSingleton<ICleaner, DenoCleaner>();

        // Python
        services.AddSingleton<ICleaner, PipCleaner>();
        services.AddSingleton<ICleaner, PipenvCleaner>();
        services.AddSingleton<ICleaner, PoetryCleaner>();
        services.AddSingleton<ICleaner, CondaCleaner>();
        services.AddSingleton<ICleaner, PdmCleaner>();
        services.AddSingleton<ICleaner, UvCleaner>();
        services.AddSingleton<ICleaner, PipxCleaner>();
        services.AddSingleton<ICleaner, PreCommitCleaner>();

        // Rust
        services.AddSingleton<ICleaner, CargoCleaner>();
        services.AddSingleton<ICleaner, RustupCleaner>();
        services.AddSingleton<ICleaner, SccacheCleaner>();

        // Go
        services.AddSingleton<ICleaner, GoCleaner>();

        // Machine learning
        services.AddSingleton<ICleaner, MlCacheCleaner>();
        services.AddSingleton<ICleaner, WandbCleaner>();

        // JVM / Android
        services.AddSingleton<ICleaner, GradleCleaner>();
        services.AddSingleton<ICleaner, MavenCleaner>();
        services.AddSingleton<ICleaner, SbtIvyCleaner>();
        services.AddSingleton<ICleaner, KonanCleaner>();
        services.AddSingleton<ICleaner, AndroidSdkCleaner>();

        // Mobile (React Native / Expo)
        services.AddSingleton<ICleaner, ReactNativeCleaner>();
        services.AddSingleton<ICleaner, ExpoCleaner>();
        services.AddSingleton<ICleaner, CocoaPodsCleaner>();

        // Other languages
        services.AddSingleton<ICleaner, GemBundlerCleaner>();
        services.AddSingleton<ICleaner, ComposerCleaner>();
        services.AddSingleton<ICleaner, PubCleaner>();
        services.AddSingleton<ICleaner, HexMixCleaner>();
        services.AddSingleton<ICleaner, VcpkgCleaner>();
        services.AddSingleton<ICleaner, CabalStackCleaner>();
        services.AddSingleton<ICleaner, ConanCleaner>();
        services.AddSingleton<ICleaner, ZigCleaner>();
        services.AddSingleton<ICleaner, SwiftPmCleaner>();
        services.AddSingleton<ICleaner, OpamCleaner>();
        services.AddSingleton<ICleaner, CpanmCleaner>();
        services.AddSingleton<ICleaner, JuliaCleaner>();
        services.AddSingleton<ICleaner, RubyGemsCleaner>();
        services.AddSingleton<ICleaner, RenvCleaner>();
        services.AddSingleton<ICleaner, LuaRocksCleaner>();
        services.AddSingleton<ICleaner, NimCleaner>();
        services.AddSingleton<ICleaner, TexLiveCleaner>();

        // Build / monorepo caches
        services.AddSingleton<ICleaner, CcacheCleaner>();
        services.AddSingleton<ICleaner, BazelCleaner>();
        services.AddSingleton<ICleaner, TurboNxCleaner>();
        services.AddSingleton<ICleaner, NodeModulesCacheCleaner>();

        // Containers / IaC
        services.AddSingleton<ICleaner, DockerCleaner>();
        services.AddSingleton<ICleaner, TerraformCleaner>();
        services.AddSingleton<ICleaner, PodmanCleaner>();
        services.AddSingleton<ICleaner, HelmCleaner>();
        services.AddSingleton<ICleaner, MinikubeCleaner>();
        services.AddSingleton<ICleaner, VagrantCleaner>();
        services.AddSingleton<ICleaner, PulumiCleaner>();
        services.AddSingleton<ICleaner, KubectlCleaner>();
        services.AddSingleton<ICleaner, AnsibleCleaner>();
        services.AddSingleton<ICleaner, LimaCleaner>();

        // IDEs / editors
        services.AddSingleton<ICleaner, JetBrainsCleaner>();
        services.AddSingleton<ICleaner, VsCodeCleaner>();
        services.AddSingleton<ICleaner, VisualStudioCleaner>();
        services.AddSingleton<ICleaner, XcodeCleaner>();
        services.AddSingleton<ICleaner, ZedCleaner>();
        services.AddSingleton<ICleaner, NeovimCleaner>();

        // Tooling downloads
        services.AddSingleton<ICleaner, BrowserAutomationCleaner>();
        services.AddSingleton<ICleaner, ElectronCacheCleaner>();
        services.AddSingleton<ICleaner, AzureFunctionsToolsCleaner>();
        services.AddSingleton<ICleaner, DotslashCleaner>();
        services.AddSingleton<ICleaner, CorepackCleaner>();
        services.AddSingleton<ICleaner, NvmCleaner>();
        services.AddSingleton<ICleaner, MiseCleaner>();
        services.AddSingleton<ICleaner, AsdfCleaner>();
        services.AddSingleton<ICleaner, SdkmanCleaner>();
        services.AddSingleton<ICleaner, NodeGypCleaner>();
        services.AddSingleton<ICleaner, GcloudCleaner>();
        services.AddSingleton<ICleaner, SonarCleaner>();

        // Project-local
        services.AddSingleton<ICleaner, BuildArtifactCleaner>();

        // Game development
        services.AddSingleton<ICleaner, UnityCleaner>();
        services.AddSingleton<ICleaner, UnrealCleaner>();
    }
}
