# Usage

`cleaner` is interactive. Run it with no arguments and drive everything from the menu:

```bash
cleaner
```

Nothing is ever deleted unattended: every action is chosen from the menu, previewed, and confirmed.
The one subcommand is `update`, because you may need it before the menu is useful.

## The menu

```text
What would you like to do?
> Clean caches
  Preview only (nothing is deleted)
  List all cleaners
  Check for updates
  Exit
```

**Clean caches** opens a grouped multi-select of every cleaner that applies to your OS. Toggle
entries with the spacebar (toggle **All cleaners**, or a category heading, to select in bulk), press
Enter, review the reclaimable-space table, and confirm. Nothing is removed until you answer that
prompt.

**Preview only** runs exactly the same scan and prints the same table, then stops. Use it to see
where your disk went before deciding what to clear.

**List all cleaners** shows every cleaner with its id, category, and status:

- **available** — the tool/paths exist on this machine.
- **not found** — nothing to clean (tool not installed or cache empty).
- **needs admin** — requires elevation; re-run as administrator/root to include it.
- **n/a (other OS)** — not applicable on the current operating system.

**Check for updates** asks GitHub whether a newer release exists and, on confirmation, downloads the
prebuilt binary for your platform, replaces the running executable in place, and relaunches it.
Cleaner only touches the network here — never during a normal clean. On Windows the previous binary
is briefly kept as `cleaner.exe.old` and removed automatically on the next run.

> If `cleaner` lives in a write-protected location (e.g. `Program Files`), run the update from an
> elevated shell so it can replace the binary. Auto-update reads the latest **published** GitHub
> release; draft releases are ignored.

The menu reopens after each action, so you can preview, then clean, then list without restarting.

## Flags

Only the two things the menu can't reasonably ask for are flags, plus the built-ins:

| Option | Alias | Description |
| --- | --- | --- |
| `--path <dir>` | `-p` | Root for project-local cleaners. Repeatable, to sweep several workspaces. Defaults to the current directory. |
| `--verbose` | `-v` | Show the individual directories behind each cleaner's size. |
| `--version` | | Print the installed version and exit. No network access. |
| `--help` | `-h` | Print usage and exit. |

```bash
cleaner --verbose
cleaner -p ~/source -p ~/work
cleaner --version
```

## `cleaner update`

The same thing the menu's **Check for updates** does, available directly so you can update without
opening the menu:

```bash
cleaner update             # check, then prompt to download & install
cleaner update --check     # only report current vs. latest; install nothing
```

## Cleaners with a trade-off

Most cleaners remove something that is simply re-downloaded or rebuilt. A few cost more than that —
deleting `C:\Windows.old` gives up the ability to roll back a Windows upgrade, for instance. Those
carry their own warning and their own yes/no prompt, asked separately just before the run-wide
confirmation, so you can decline one without cancelling the whole clean.

## Elevation

Some OS cleaners (Windows Update cache, system temp, Delivery Optimization, the systemd journal, and
some system package managers) need administrator/root privileges. When not elevated, Cleaner lists
them as **needs admin** and skips them during a run with a clear note — re-run from an elevated shell
to include them.

## Project-local cleaners

The `build-artifacts` cleaner and a few others act on a directory tree rather than a global cache.
Point them at your code with `--path`, then pick them from the menu:

```bash
cleaner --path ./my-repo
```

## Logs

Cleaner logs each run, plus any errors and crashes, to **`~/.cleaner/logs/cleaner.log`**.

If one cleaner fails, the rest still run — the failure shows in the summary and the log path is
printed so you can see the details.

## Exit codes

- `0` — success (including previews and "nothing to clean").
- `1` — no interactive terminal was attached, or one or more cleaners reported errors.
- `130` — cancelled with Ctrl+C.
