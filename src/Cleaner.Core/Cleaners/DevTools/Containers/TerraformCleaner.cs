using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Terraform provider plugin cache.</summary>
public sealed class TerraformCleaner : DirectoryCleanerBase
{
    public override string Id => "terraform";

    public override string Name => "Terraform plugin cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".terraform.d", "plugin-cache"), DeleteMode.ClearContents)];
}
