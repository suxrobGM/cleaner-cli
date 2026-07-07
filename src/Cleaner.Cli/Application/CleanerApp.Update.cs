using System.Runtime.InteropServices;
using Cleaner.Core.Services;
using Spectre.Console;

namespace Cleaner.Cli.Application;

public sealed partial class CleanerApp
{
    public async Task<int> UpdateAsync(bool checkOnly, bool assumeYes, CancellationToken cancellationToken)
    {
        UpdateCheckResult check;
        try
        {
            check = await renderer.StatusAsync("Checking for updates…", updateService.CheckAsync, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            renderer.Line($"[red]Could not reach the update server:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        renderer.Line($"Current version: [bold]{check.CurrentVersion.EscapeMarkup()}[/]");

        if (!check.IsUpdateAvailable || check.LatestVersion is null)
        {
            var latest = check.LatestVersion ?? check.CurrentVersion;
            renderer.Line($"[green]You're on the latest version ({latest.EscapeMarkup()}).[/]");
            return 0;
        }

        renderer.Line($"Latest version:  [bold green]{check.LatestVersion.EscapeMarkup()}[/]");
        if (check.ReleaseUrl is { Length: > 0 } url)
        {
            renderer.Line($"[grey]Release notes: {url.EscapeMarkup()}[/]");
        }

        if (checkOnly)
        {
            renderer.Line("[grey]Run 'cleaner update' to install it.[/]");
            return 0;
        }

        if (check.Asset is null)
        {
            renderer.Line(
                $"[yellow]No prebuilt binary is available for this platform ({RuntimeInformation.RuntimeIdentifier.EscapeMarkup()}).[/]");
            if (check.ReleaseUrl is { Length: > 0 } releaseUrl)
            {
                renderer.Line($"[grey]Download it manually from {releaseUrl.EscapeMarkup()}[/]");
            }

            return 1;
        }

        if (!assumeYes &&
            !renderer.Confirm($"Update from [bold]{check.CurrentVersion.EscapeMarkup()}[/] to [bold green]{check.LatestVersion.EscapeMarkup()}[/]?"))
        {
            renderer.Line("[grey]Cancelled.[/]");
            return 0;
        }

        try
        {
            await renderer.DownloadAsync(
                "Downloading update",
                (progress, ct) => updateService.ApplyAsync(check, progress, ct),
                cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
        {
            renderer.Line($"[red]Update failed:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        renderer.Line($"[green]Updated to {check.LatestVersion.EscapeMarkup()}.[/] Relaunching…");
        return 0;
    }
}
