# Usage

Cleaner is interactive only. Run `cleaner` in a terminal; without one it exits with code 1.

## Menu

| Item | What it does |
| --- | --- |
| **Preview and clean caches** | Select cleaners, preview sizes, confirm, and clean. See [Clean flow](#clean-flow). |
| **List all cleaners** | Show every cleaner with its id, category, and status. |
| **Check for updates** | Download, install, and relaunch the latest release. |
| **Exit** | Quit. |

The menu returns after each action. List statuses:

| Status | Meaning |
| --- | --- |
| available | The tool or its folders exist on this machine. |
| not found | Nothing to clean. |
| needs admin | Run Cleaner elevated (administrator or root) to include it. |
| n/a (other OS) | Not for this operating system. |

## Options

| Option | Description |
| --- | --- |
| `--path <dir>`, `-p` | Folder to search for projects. Repeatable. Defaults to the current folder. |
| `--verbose`, `-v` | Show the folders behind each cleaner's size. |
| `--version` | Print the version. No network access. |
| `--help`, `-h` | Print usage. |
| `update` | Check for a newer release and offer to install it. |
| `update --check` | Report the current and latest versions only. |

```bash
cleaner -p ~/source -p ~/work
cleaner update --check
```

Updates use the latest published GitHub release (no drafts or prereleases). That is the only
network access. If `cleaner` sits in a protected folder such as `Program Files`, update from an
elevated shell. On Windows the old binary is kept as `cleaner.exe.old` and removed on the next run.

## Clean flow

1. **Select.** Tick cleaners, a category, or **All cleaners**. Cleaners for other operating systems
   are hidden.
2. **Scan.** Cleaners that need admin are skipped and listed. Nothing is deleted.
3. **Preview.** A table shows each cleaner's size, plus notes on protected paths and refused
   folders. Some command-driven cleaners show *n/a* and report their size after running.
4. **Warnings.** A cleaner with a real trade-off (marked **asks again** in
   [cleaners.md](cleaners.md)) explains it and asks on its own. Declining drops only that cleaner.
5. **Pick folders (optional).** When a cleaner found several folders, you can list them and untick
   the ones to keep. Unticked folders are protected for the whole run. Cleaners that run a tool's
   own command are all-or-nothing and are not offered here.
6. **Confirm.** One prompt for the total.
7. **Clean.** Cleaner removes exactly the approved targets. It never looks for new ones.
8. **Summary.** Freed space, skipped paths, errors, and tool checks.

If one cleaner fails, the rest still run.

## Project folders

Some cleaners work inside your projects instead of a global cache:

- `build-artifacts` and `unity` search every `--path` folder.
- `node-modules-cache`, `turbo-nx`, `visualstudio`, and `expo` use the first `--path` folder (or the
  current folder).

They refuse your home folder, a drive root, and system or install folders. A new terminal usually
opens in your home folder, so pass `--path` or `cd` into a workspace first. The preview says when a
folder was refused.

`build-artifacts` is opt-in. It sits under **Project folders**, outside **All cleaners**, and takes a
folder only with proof that its project generated it. See [the rules](cleaners.md#project-local).

## What Cleaner never deletes

Every path is checked when scanned and again just before removal. The guard refuses:

- folders on PATH (process, user, and machine), their parents, and anything inside them;
- essential folders and anything that contains them: drive roots, your home folder,
  Documents/Desktop/Downloads and similar, AppData, the Windows folder, `ProgramData`, and on
  macOS/Linux `/usr`, `/opt`, `/etc`, `/Applications`, `~/Library`, `~/.config`, `~/.local`, and
  similar (caches inside them, such as `Windows\Temp`, can still be cleared);
- installed tools: `Program Files`, `%LOCALAPPDATA%\Programs`, Scoop apps, WinGet packages, Cargo,
  rustup, nvm, Volta, pyenv, asdf, mise, SDKMAN, Homebrew, editor extensions, `~/.local/bin`, and
  more (only their cache folders may be cleared);
- personal config such as `~/.ssh`, `~/.aws`, `~/.docker`, and `~/.cleaner`;
- `.git` folders and git repositories;
- links and junctions, and paths that resolve through a link to a protected place;
- anything in `~/.cleaner/exclude.txt`;
- folders you unticked in the picker.

### Exclude list

List extra folders in `~/.cleaner/exclude.txt`, one per line. `~`, `%VAR%`, and `$VAR` are expanded;
`#` starts a comment. The list only adds protection.

```text
# models I downloaded by hand
~/models
%USERPROFILE%\datasets
```

### How deletion works

- A folder is renamed aside (`name.cleaner-deleting-xxxx`), then deleted. On Windows the rename fails
  while any file inside is open, so an in-use folder is skipped whole, never half-deleted.
- A folder holding a running program is skipped on every platform.
- If a delete fails after the rename, the leftover is recorded in `~/.cleaner/pending-deletes.txt`.
  The `unfinished-deletes` cleaner finishes it on a later run.
- When a tool has its own cleanup command (`npm cache clean`, `uv cache clean`,
  `dotnet nuget locals`, ...), Cleaner runs it instead of deleting folders. If the command fails,
  the error is reported and nothing is deleted in its place.
- Before and after a clean, Cleaner checks that common tools (git, node, dotnet, cargo, python, ...)
  still run and that no PATH folder lost files. Problems are shown in red and logged.

## Elevation

Some OS cleaners (Windows Update, Windows temp, Delivery Optimization, the systemd journal, some
package managers) need admin or root. Without it they are listed as **needs admin** and skipped.

## Logs

| File | Contents |
| --- | --- |
| `~/.cleaner/logs/cleaner.log` | Run summaries: cleaners run, admin skips, scan roots, space freed, errors, crashes. |
| `~/.cleaner/logs/runs/run-<timestamp>.log` | One per clean: the approved plan, then every path removed or skipped (with the reason) and every error. The last 30 are kept. |

The summary prints the run log's path.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success, including "nothing to clean" and a declined prompt. |
| `1` | No interactive terminal, a cleaner reported errors, or a tool broke during the clean. |
| `130` | Cancelled with Ctrl+C. |
