using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class OsApplicabilityTests
{
    [Fact]
    public void Windows_only_cleaner_is_not_applicable_on_linux()
    {
        var env = new FakeEnvironment { Os = OsPlatform.Linux };
        var context = TestContext.Create(new FakeFileSystem(), env);

        Assert.False(new WindowsUpdateCacheCleaner().IsApplicable(context));
        Assert.True(new WindowsUpdateCacheCleaner().RequiresElevation);
    }

    [Fact]
    public void Posix_only_cleaners_are_not_applicable_on_windows()
    {
        var context = TestContext.Create(new FakeFileSystem(), new FakeEnvironment { Os = OsPlatform.Windows });

        Assert.False(new SwiftPmCleaner().IsApplicable(context));
        Assert.False(new OpamCleaner().IsApplicable(context));
        Assert.False(new CpanmCleaner().IsApplicable(context));
        Assert.False(new NvmCleaner().IsApplicable(context));
        Assert.False(new AsdfCleaner().IsApplicable(context));
        Assert.False(new SdkmanCleaner().IsApplicable(context));
        Assert.False(new CocoaPodsCleaner().IsApplicable(context));
        Assert.False(new TexLiveCleaner().IsApplicable(context));
        Assert.False(new AnsibleCleaner().IsApplicable(context));
        Assert.False(new LimaCleaner().IsApplicable(context));
    }

    [Fact]
    public void Windows_only_cleaners_are_not_applicable_on_linux()
    {
        var context = TestContext.Create(new FakeFileSystem(), new FakeEnvironment { Os = OsPlatform.Linux });

        Assert.False(new WingetCleaner().IsApplicable(context));
        Assert.False(new GpuInstallerLeftoverCleaner().IsApplicable(context));
        Assert.False(new WinSxSCleaner().IsApplicable(context));
        Assert.False(new WindowsOldCleaner().IsApplicable(context));
        Assert.False(new OneDriveCleaner().IsApplicable(context));
    }
}
