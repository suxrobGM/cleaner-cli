---
name: release
description: Cut a new Cleaner release — bump the version in Directory.Build.props, roll the CHANGELOG, commit, and create + push the matching git tag that triggers the Release workflow. Use when the user asks to "release", "cut a release", "ship a version", or "bump the version".
---

# Release Cleaner

Cuts a release of the `cleaner` CLI. The version lives in **one place**
(`Directory.Build.props` → `<Version>`); pushing a `vX.Y.Z` tag fires
`.github/workflows/release.yml`, which builds the Native AOT binaries and publishes
a GitHub Release whose notes come from the matching `CHANGELOG.md` section.

## Inputs

The user may name a version (`1.2.0`) or a bump kind (`patch` / `minor` / `major`).
If neither is given, propose a bump from the unreleased changes and confirm before tagging.

## Steps

1. **Pre-flight.**
   - Ensure the working tree is clean (`git status --porcelain`). If not, stop and report.
   - Read the current version from `Directory.Build.props` (`<Version>`).
   - Confirm you're on the default branch (`main`) and up to date with the remote.

2. **Decide the new version.** Follow SemVer:
   - `major` for breaking changes, `minor` for new cleaners/features, `patch` for fixes.
   - Infer the bump from the `## [Unreleased]` section of `CHANGELOG.md` and/or the commits
     since the last tag (`git log <lastTag>..HEAD --oneline`). State the chosen version and why.

3. **Bump the version.** Edit `Directory.Build.props`, replacing the `<Version>` value
   with the new `X.Y.Z` (no `v` prefix here).

4. **Roll the CHANGELOG.** In `CHANGELOG.md`:
   - Rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD` using **today's date**
     (do not guess — get it with `date +%F`).
   - Add a fresh empty `## [Unreleased]` section above it.
   - If `## [Unreleased]` had no entries, summarize the commits since the last tag into
     `Added` / `Changed` / `Fixed` groups (Keep a Changelog style).
   - Update the link-reference footer: add an `[X.Y.Z]` compare link and point
     `[Unreleased]` at `vX.Y.Z...HEAD`.

5. **Build & verify.** Run `dotnet build` (warnings are errors) and `dotnet test`.
   If either fails, stop and report — do not tag a broken build.

6. **Commit.** Stage `Directory.Build.props` and `CHANGELOG.md` and commit:
   `chore(release): vX.Y.Z`.

7. **Tag.** Create an annotated tag matching the version exactly:
   `git tag -a vX.Y.Z -m "Release vX.Y.Z"`. The tag's `v` prefix is required —
   the workflow triggers on `tags: [ 'v*' ]`.

8. **Push (confirm first).** This step publishes a release, so confirm with the user
   unless they already said to push. Then:
   `git push origin main` and `git push origin vX.Y.Z`.

9. **Report.** Print the new version, the tag, and the Actions URL
   (`https://github.com/suxrobGM/cleaner-cli/actions`) so the user can watch the release build.

## Notes

- The release body is taken verbatim from the `## [X.Y.Z]` block in `CHANGELOG.md`,
  so write it for end users. Missing/empty section ⇒ the workflow falls back to
  auto-generated notes.
- Never edit a version number anywhere but `Directory.Build.props`.
- If the tag already exists, stop — never force-move a published release tag.
