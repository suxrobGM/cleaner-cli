using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Prunes Docker disk: <c>system prune -f</c> + <c>builder prune -af</c> by default; with
/// <c>--force</c>, <c>system prune -a --volumes -f</c> also removes unused images and named volumes
/// (can delete data), so it stays behind the cautious-targets flag. On Docker Desktop/WSL2 this only
/// frees space inside the <c>.vhdx</c>; the host file shrinks after a separate compaction.
/// </summary>
public sealed class DockerCleaner : ProcessCleanerBase
{
    public override string Id => "docker";

    public override string Name => "Docker (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "docker";

    // The declared safe command, used for the dry-run/missing-tool fallback path.
    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "--force"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return context.Force
            ? ["system", "prune", "-a", "--volumes", "--force"]
            : ["system", "prune", "--force"];
        yield return ["builder", "prune", "--all", "--force"];
    }

    // Docker's storage lives in the daemon's data root, which isn't a user-accessible directory we
    // can size or delete — the prune commands are the only safe interface.
    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

/// <summary>Terraform provider plugin cache.</summary>
public sealed class TerraformCleaner : DirectoryCleanerBase
{
    public override string Id => "terraform";

    public override string Name => "Terraform plugin cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".terraform.d", "plugin-cache"), DeleteMode.ClearContents)];
}

/// <summary>Prunes Podman storage; like Docker, the prune commands are the only safe interface.</summary>
public sealed class PodmanCleaner : ProcessCleanerBase
{
    public override string Id => "podman";

    public override string Name => "Podman (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "podman";

    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "--force"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return context.Force
            ? ["system", "prune", "-a", "--volumes", "--force"]
            : ["system", "prune", "--force"];
    }

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

/// <summary>Helm chart repository cache (indexes and downloaded charts).</summary>
public sealed class HelmCleaner : DirectoryCleanerBase
{
    public override string Id => "helm";

    public override string Name => "Helm repository cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "HELM_REPOSITORY_CACHE") is { } configured)
        {
            yield return new CleanupPath(configured);
            yield break;
        }

        // Helm keeps its cache under the temp dir on Windows and the cache root elsewhere.
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.TempDirectory, "helm"))
            : new CleanupPath(Path.Combine(env.CacheDirectory, "helm"));
    }
}

/// <summary>minikube image/ISO download cache (keeps profiles and VMs).</summary>
public sealed class MinikubeCleaner : DirectoryCleanerBase
{
    public override string Id => "minikube";

    public override string Name => "minikube cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "MINIKUBE_HOME") ?? env.HomePath(".minikube");
        yield return new CleanupPath(Path.Combine(root, "cache"));
    }
}

/// <summary>Vagrant temp downloads only — boxes are user data and are never touched.</summary>
public sealed class VagrantCleaner : DirectoryCleanerBase
{
    public override string Id => "vagrant";

    public override string Name => "Vagrant temp downloads";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "VAGRANT_HOME") ?? env.HomePath(".vagrant.d");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents);
    }
}

/// <summary>Pulumi provider plugin binaries (re-downloaded on demand; keeps stacks and config).</summary>
public sealed class PulumiCleaner : DirectoryCleanerBase
{
    public override string Id => "pulumi";

    public override string Name => "Pulumi plugin cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "PULUMI_HOME") ?? env.HomePath(".pulumi");
        yield return new CleanupPath(Path.Combine(root, "plugins"), DeleteMode.ClearContents);
    }
}

/// <summary>kubectl discovery and HTTP caches (kubeconfig and credentials untouched).</summary>
public sealed class KubectlCleaner : DirectoryCleanerBase
{
    public override string Id => "kubectl";

    public override string Name => "kubectl caches";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".kube", "cache"), Description: "discovery cache");
        yield return new CleanupPath(env.HomePath(".kube", "http-cache"), Description: "HTTP cache");
    }
}

/// <summary>Ansible temp workspace and Galaxy download cache.</summary>
public sealed class AnsibleCleaner : DirectoryCleanerBase
{
    public override string Id => "ansible";

    public override string Name => "Ansible caches";

    public override string Category => Categories.Containers;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".ansible", "tmp"), Description: "temp workspace");
        yield return new CleanupPath(env.HomePath(".ansible", "galaxy_cache"), Description: "Galaxy cache");
    }
}

/// <summary>Lima VM image download cache (used by lima and colima on macOS/Linux).</summary>
public sealed class LimaCleaner : DirectoryCleanerBase
{
    public override string Id => "lima";

    public override string Name => "Lima image cache";

    public override string Category => Categories.Containers;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsMacOs
            ? new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "lima"))
            : new CleanupPath(Path.Combine(env.CacheDirectory, "lima"));
    }
}
