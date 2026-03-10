# Release and Versioning

This document is the source of truth for UniCli versioning and release flow.

UniCli is not treated as a single repo-wide versioned artifact. Versions are scoped to the artifact that is actually shipped.

## Versioning Model

The following version streams are independent:

| Artifact | Tag format | Version source | Bump when |
| --- | --- | --- | --- |
| CLI binary | `cli/vX.Y.Z` | `src/UniCli.Client/UniCli.Client.csproj` | CLI behavior, packaging, install/update UX, shell-facing output, or distribution changes |
| Unity server package | `server/vX.Y.Z` | `src/UniCli.Unity/Packages/com.yucchiy.unicli-server/package.json` | Unity-side command behavior, package contents, editor/runtime integration, or package distribution changes |
| Codex Unity skill | `codex/skill/unity-development/vX.Y.Z` | `.agents/skills/unity-development/SKILL.md` | Codex-specific instructions or workflows change |
| Claude Code plugin | `claude-code/plugin/vX.Y.Z` | `.claude-plugin/marketplace.json` | Claude Code plugin packaging or marketplace distribution changes |
| Claude Code Unity skill | `claude-code/skill/unity-development/vX.Y.Z` | `.claude-plugin/unicli/skills/unity-development/SKILL.md` | Claude Code-specific instructions or workflows change |

These versions do not need to match.

By default, release the smallest affected artifact set:

- A CLI-only fix should only bump the CLI version.
- A Unity server-only fix should only bump the server version.
- A Codex-only skill update should only bump the Codex skill version.
- A Claude Code-only skill or plugin update should only bump the Claude Code artifact versions involved.

Coordinated releases are allowed when one change intentionally spans multiple artifacts, but shared version numbers are not required.

## Protocol Compatibility

`ProtocolVersion` is separate from artifact versioning.

- It exists only as a compatibility guard for breaking wire-level changes.
- Do not bump it for normal CLI, server, or skill releases.
- Only bump it when the named-pipe handshake or request/response wire shape becomes incompatible.

`ProtocolVersion` is not a release stream and is not tagged independently.

## Release Flow

### 1. Start from `main`

Create a release branch from `main`.

Use an artifact-scoped branch name:

- `release/cli/vX.Y.Z`
- `release/server/vX.Y.Z`
- `release/codex-skill-unity-development/vX.Y.Z`
- `release/claude-code-plugin/vX.Y.Z`
- `release/claude-code-skill-unity-development/vX.Y.Z`

If one PR intentionally releases multiple artifacts together, use a branch name that reflects the primary artifact and document the coordinated release in the PR description.

### 2. Bump only the affected artifact versions

Update only the files that correspond to the release artifacts in scope.

Examples:

- CLI release: bump `src/UniCli.Client/UniCli.Client.csproj`
- Server release: bump `src/UniCli.Unity/Packages/com.yucchiy.unicli-server/package.json`
- Codex skill release: bump `.agents/skills/unity-development/SKILL.md`
- Claude Code plugin release: bump `.claude-plugin/marketplace.json`
- Claude Code skill release: bump `.claude-plugin/unicli/skills/unity-development/SKILL.md`

Do not bump unrelated artifact versions just to keep numbers aligned.

### 3. Verify the artifacts being released

Run the checks that match the artifacts in scope.

Minimum expectations:

- CLI release:
  - `dotnet build src/UniCli.Protocol`
  - `dotnet publish src/UniCli.Client -o .build`
- Server release:
  - `dotnet build src/UniCli.Protocol`
  - compile and test the Unity package/project as appropriate for the change
- Skill release:
  - verify the skill text matches current commands and workflows
- Coordinated runtime release:
  - verify both CLI and Unity-side behavior relevant to the change

### 4. Merge a PR to `main`

Create a PR that describes:

- which artifacts are being released
- which versions were bumped
- any compatibility notes
- any manual follow-up required because tooling is still transitioning

### 5. Tag the released artifacts

After merge, create tags for the artifacts that were actually released.

Examples:

- `cli/v1.2.0`
- `server/v1.5.0`
- `codex/skill/unity-development/v1.3.0`
- `claude-code/plugin/v1.4.0`
- `claude-code/skill/unity-development/v1.2.0`

Do not create a repo-wide `vX.Y.Z` tag as the target policy.

## Transition Note

The repository is moving toward artifact-scoped releases, but not every artifact has dedicated automation yet.

Current expectations:

- CLI release automation uses `cli/vX.Y.Z` tags.
- `unicli install` and `unicli install --update` target the recommended `server/vX.Y.Z` tag rather than assuming a repo-wide `vX.Y.Z` tag.
- Non-CLI artifacts may still require manual release steps until dedicated automation is added.

When adding tooling, extend the artifact-scoped model defined in this document instead of reintroducing repo-wide shared versioning.
