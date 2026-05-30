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
```

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
| `--force` | `-f` | Include targets that are otherwise treated cautiously. |
| `--path <dir>` | `-p` | Base directory for project-local cleaners (defaults to the current directory). |

You can find cleaner ids with `cleaner list`.

## Elevation

Some OS cleaners (Windows Update cache, system temp, Delivery Optimization, the systemd journal,
and some system package managers) need administrator/root privileges. When not elevated, Cleaner
lists them as **needs admin** and skips them during a run with a clear note — re-run from an elevated
shell to include them.

## Project-local cleaners

The `build-artifacts` cleaner and a few others act on a directory rather than a global cache. Point
them at a project with `--path`:

```bash
cleaner clean build-artifacts --path ./my-repo --dry-run
```

## Exit codes

- `0` — success (including dry runs and "nothing to clean").
- `1` — invalid selection (unknown id / no selection), or one or more cleaners reported errors.
