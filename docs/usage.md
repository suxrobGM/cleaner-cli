# Usage

Run `cleaner` with no arguments to use the interactive menu:

```bash
cleaner
```

Nothing is deleted unattended: each action is selected, previewed, and confirmed. The `update`
subcommand is also available for updating before opening the menu.

## The menu

```text
What would you like to do?
> Preview and clean caches
  List all cleaners
  Check for updates
  Exit
```

**Preview and clean caches** opens a grouped multi-select of cleaners applicable to your OS. Toggle
entries with the spacebar (or **All cleaners** / a category heading for bulk selection), press
Enter, review the reclaimable-space table, and confirm. Nothing is removed before confirmation.

**List all cleaners** shows every cleaner with its id, category, and status:

- **available** — the tool/paths exist on this machine.
- **not found** — nothing to clean (tool not installed or cache empty).
- **needs admin** — requires elevation; re-run as administrator/root to include it.
- **n/a (other OS)** — not applicable on the current operating system.

**Check for updates** asks GitHub for a newer release and, on confirmation, downloads the platform
binary, replaces the executable, and relaunches it. Cleaner uses the network only here, never during
a normal clean. Windows briefly keeps the previous binary as `cleaner.exe.old`, removing it next run.

> If `cleaner` lives in a write-protected location (e.g. `Program Files`), run the update from an
> elevated shell so it can replace the binary. Auto-update reads the latest **published** GitHub
> release; draft releases are ignored.

The menu reopens after each action, so you can preview, clean, and list without restarting.

## Flags

Flags cover the options the menu cannot ask for, plus the built-ins:

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

The menu's **Check for updates** action is also available directly:

```bash
cleaner update             # check, then prompt to download & install
cleaner update --check     # only report current vs. latest; install nothing
```

## Cleaners with a trade-off

Most cleaners remove data that can be downloaded or rebuilt. A few have larger trade-offs: deleting
`C:\Windows.old`, for example, removes the ability to roll back a Windows upgrade. These cleaners
show a warning and separate yes/no prompt, so you can decline one without cancelling the run.

## Elevation

Some OS cleaners (Windows Update cache, system temp, Delivery Optimization, the systemd journal, and
some system package managers) need administrator/root privileges. Without elevation, Cleaner lists
them as **needs admin** and skips them; re-run from an elevated shell to include them.

## Project-local cleaners

The `build-artifacts` cleaner and some others sweep a directory tree rather than a global cache.
Point them at your code with `--path`, then select them:

```bash
cleaner --path ./my-repo
```

## Picking individual folders

A sweep over a source tree can match hundreds of folders, and taking all of them means reinstalling
dependencies for every project still in use. After the size table, a cleaner that found more than one
folder can be opened up: say yes to the folder prompt and they are listed by name (`node_modules`,
`.venv`, `.next`, ...) with their sizes. Everything starts ticked, so untick only what to keep.
Decline and the run removes everything in the table, as before.

## Logs

Cleaner logs runs, errors, and crashes to **`~/.cleaner/logs/cleaner.log`**.

If one cleaner fails, the rest still run; the summary shows the failure and prints the log path.

## Exit codes

- `0` — success (including previews and "nothing to clean").
- `1` — no interactive terminal was attached, or one or more cleaners reported errors.
- `130` — cancelled with Ctrl+C.
