---
name: release
description: Cut a Cleaner release by updating Directory.Build.props and CHANGELOG.md, then committing and tagging it. Use when the user asks to release, ship, or bump a version.
---

# Release Cleaner

The version lives only in `Directory.Build.props`. Pushing a `vX.Y.Z` tag runs
`.github/workflows/release.yml`, which builds the Native AOT binaries and publishes a GitHub Release
whose body is the matching `CHANGELOG.md` section.

## Input

An explicit version, or `patch`, `minor`, or `major`. With neither, propose a bump from the
unreleased changes and confirm it before tagging.

## Steps

1. Preflight: the working tree is clean (`git status --porcelain`), the branch is `main` and up to
   date with its remote. Read the current `<Version>` from `Directory.Build.props`.
2. Pick the version by SemVer (`major` breaking, `minor` features, `patch` fixes), using
   `CHANGELOG.md` and `git log <lastTag>..HEAD --oneline`. State the version and why.
3. Set `<Version>` in `Directory.Build.props` to `X.Y.Z` (no `v`).
4. Roll `CHANGELOG.md`:
   - Rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD` (date from `date +%F`).
   - Add an empty `## [Unreleased]` above it.
   - If Unreleased was empty, summarize the commits since the last tag under Keep a Changelog
     headings.
   - Add the `[X.Y.Z]` compare link and point `[Unreleased]` at `vX.Y.Z...HEAD`.
5. Run `dotnet build` and `dotnet test`. Stop if either fails.
6. Commit only those two files as `chore(release): vX.Y.Z`.
7. Tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`.
8. Ask before pushing unless the user already approved it, then:

   ```bash
   git push origin main
   git push origin vX.Y.Z
   ```

9. Report the version, tag, and `https://github.com/suxrobGM/cleaner-cli/actions`.

## Rules

- Write the release section for end users; it becomes the release body. A missing or empty section
  makes the workflow use generated notes.
- Never change the version anywhere but `Directory.Build.props`.
- Never move an existing release tag.
