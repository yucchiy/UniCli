# Agent Guidance

## Versioning and Releases

The source of truth for versioning and release flow is:

- `doc/release-versioning.md`

Default assumptions for this repo:

- Do not assume a single repo-wide version number.
- Treat the CLI, Unity server package, Codex skill, Claude Code plugin, and Claude Code skill as separate release artifacts.
- Treat `ProtocolVersion` as a compatibility guard only, not as a normal release version.
- If asked to bump a version, change only the targeted artifact versions unless the request explicitly calls for a coordinated multi-artifact release.

If release tooling or existing commands still imply the old shared `vX.Y.Z` flow, treat that as transitional and follow `doc/release-versioning.md` as the intended policy.
