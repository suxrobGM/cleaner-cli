# Usage

`cleaner` has three explicit commands plus an interactive mode when run with no arguments.

## Interactive mode

```bash
cleaner
```

Shows a banner, then a grouped multi-select of every cleaner that applies to your OS. Pick what you
want with the spacebar, press Enter, review the reclaimable-space preview, and confirm. This is the
friendliest way to use Cleaner.

## `cleaner list`

Lists every cleaner with its id, category, and status:

- **available** — the tool/paths exist on this machine.
- **not found** — nothing to clean (tool not installed or cache empty).
- **needs admin** — requires elevation; re-run as administrator/root to include it.
- **n/a (other OS)** — not applicable on the current operating system.

## `cleaner scan [cleaners...]`

Measures reclaimable space without deleting anything. Accepts the same selection options as `clean`.

```bash
cleaner scan nuget npm
cleaner scan --category "Python"
cleaner scan --all
cleaner scan --all --json        # machine-readable report for scripts/CI
cleaner scan --all --verbose     # per-directory breakdown behind each size
```

With `--json`, stdout is a single JSON document (`totalBytes` plus per-cleaner `id`, `name`,
`category`, `bytes`, and `targets`) — pipe it to `jq` or `ConvertFrom-Json`. Command-based cleaners
(docker, winsxs, …) can't be measured up front; tables show them as *n/a (runs command)* and JSON
marks them with `"sizeIsEstimatable": false`.

## `cleaner clean [cleaners...]`

Scans, previews, confirms, then deletes.

```bash
cleaner clean nuget gradle          # specific cleaners (by id)
cleaner clean --all                 # every applicable cleaner
cleaner clean --category "IDEs / editors"
```

### Options

| Option | Alias | Description |
| --- | --- | --- |
| `--all` | `-a` | Act on every applicable cleaner. |
| `--category <name>` | `-c` | Act on all cleaners in a category. |
| `--dry-run` | `-n` | Preview reclaimable space; delete nothing. |
| `--yes` | `-y` | Skip the confirmation prompt. |
| `--force` | `-f` | Include targets that are otherwise treated cautiously (e.g. `windows-old`, docker volumes). |
| `--path <dir>` | `-p` | Base directory for project-local cleaners (defaults to the current directory). |
| `--verbose` | `-v` | Show the individual directories behind each cleaner's size. |
| `--json` | | (`scan` only) Emit the report as JSON on stdout. |

You can find cleaner ids with `cleaner list`. An unknown `--category` prints the list of valid
category names.

## `cleaner --version`

Prints the installed version and exits. No network access.

```bash
cleaner --version          # e.g. 1.0.0
```

## `cleaner update`

Checks GitHub for a newer release and, on confirmation, installs it. Cleaner only contacts the
network when you run this command — never on a normal run.

```bash
cleaner update             # check, then prompt to download & install
cleaner update --check     # only report current vs. latest; install nothing
cleaner update --yes       # update without the confirmation prompt
```

`update` downloads the prebuilt binary for your platform, replaces the running `cleaner` executable
in place, and relaunches it to confirm the new version. On Windows the previous binary is briefly
kept as `cleaner.exe.old` and removed automatically on the next run.

> **Notes.** Auto-update reads the latest **published** GitHub release (the release workflow
> publishes automatically when a `v*` tag is pushed; draft releases are ignored). If `cleaner` lives
> in a write-protected location (e.g. `Program Files`), re-run the update from an elevated shell so it
> can replace the binary.

## Elevation

Some OS cleaners (Windows Update cache, system temp, Delivery Optimization, the systemd journal,
and some system package managers) need administrator/root privileges. When not elevated, Cleaner
lists them as **needs admin** and skips them during a run with a clear note — re-run from an elevated
shell to include them.

## Logs

Cleaner logs each run, plus any errors and crashes, to **`~/.cleaner/logs/cleaner.log`**.

If one cleaner fails, the rest still run — the failure shows in the summary and the log path is
printed so you can see the details.

## Project-local cleaners

The `build-artifacts` cleaner and a few others act on a directory rather than a global cache. Point
them at a project with `--path`:

```bash
cleaner clean build-artifacts --path ./my-repo --dry-run
```

## Scripts, CI, and redirected output

When output is redirected (or no terminal is attached), Cleaner skips the banner, spinners, and
progress bars and never prompts: the interactive menu exits with guidance, and `clean` requires
`--yes`. Use `scan --json` for machine-readable results.

## Exit codes

- `0` — success (including dry runs and "nothing to clean").
- `1` — invalid selection (unknown id/category, no selection), a non-interactive `clean` without
  `--yes`, or one or more cleaners reported errors.
- `130` — cancelled with Ctrl+C.
