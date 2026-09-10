---
name: release
description: Cut a Cleaner release by updating Directory.Build.props and CHANGELOG.md, then committing and tagging it. Use when the user asks to release, ship, or bump a version.
---

# Release Cleaner

Cleaner’s version is defined only in `Directory.Build.props`. Pushing a `vX.Y.Z` tag triggers
`.github/workflows/release.yml`, which builds the Native AOT binaries and publishes a GitHub Release
from the matching `CHANGELOG.md` section.

## Input

Accept an explicit version or a `patch`, `minor`, or `major` bump. If neither is provided, propose a
bump from the unreleased changes and confirm it before tagging.

## Procedure

1. Preflight:
   - Require a clean working tree (`git status --porcelain`).
   - Read the current `<Version>` from `Directory.Build.props`.
   - Confirm the branch is `main` and up to date with its remote.
2. Choose the version using SemVer:
   - `major` for breaking changes, `minor` for features, and `patch` for fixes.
   - Use `CHANGELOG.md` and `git log <lastTag>..HEAD --oneline` as evidence.
   - State the chosen version and rationale.
3. Replace `<Version>` in `Directory.Build.props` with `X.Y.Z` (without a `v` prefix).
4. Roll `CHANGELOG.md`:
   - Rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD`, using today’s date from `date +%F`.
   - Add a new empty `## [Unreleased]` above it.
   - If Unreleased was empty, summarize commits since the last tag under Keep a Changelog headings.
   - Add the `[X.Y.Z]` comparison link and point `[Unreleased]` to `vX.Y.Z...HEAD`.
5. Run `dotnet build` and `dotnet test`. Stop if either fails.
6. Commit only `Directory.Build.props` and `CHANGELOG.md` as `chore(release): vX.Y.Z`.
7. Create the annotated tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`.
8. Before publishing, ask for confirmation unless the user already authorized a push. Then run:

   ```bash
   git push origin main
   git push origin vX.Y.Z
   ```

9. Report the version, tag, and `https://github.com/suxrobGM/cleaner-cli/actions`.

## Safety

- Write the release section for end users; it becomes the release body. An absent or empty section
  causes the workflow to use generated notes.
- Never change the version outside `Directory.Build.props`.
- Never force-move an existing release tag.
