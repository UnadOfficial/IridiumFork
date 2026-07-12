# Repository Guidelines

## Scope

These instructions apply to the whole repository. More specific `AGENTS.md` files in subdirectories override this file for their subtree.

## Project Layout

- `main/`: core Iridium mod code.
- `loaders/`: loader-specific entry points for MelonLoader and UnityModManager.
- `frontline/`: companion frontend/configuration tooling and UI resources.
- `architectury/Iris.Iml/`: IML UI library project.
- `lib/`: local Unity/game dependency assemblies used by project references.
- `scripts/`, `build.sh`, `install-requirements.sh`: build and environment helpers.
- `Iridium.slnx`: solution file covering the main, loader, frontline, and Iris.Iml projects.

## Development Rules

- Keep changes scoped to the requested area. Do not reformat or rewrite unrelated files.
- Preserve public entry points, loader contracts, serialized config names, and resource paths unless the task explicitly calls for a breaking change.
- Treat `frontline/Resources/ui/*.iml` as contract-sensitive. Before changing UI context names or handlers, search for the corresponding `.iml` bindings and C# handler registrations.
- For `frontline` settings work, preserve existing binding names such as `settings.*`, `defaultMusicPath`, and `fastMusicPath` unless all consumers are updated together.
- Remember to update the version number in `Info.json` when a change requires a version bump or release packaging update.
- Do not modify generated output directories such as `bin/`, `obj/`, or `out/` unless the user specifically asks for build artifacts.
- This repository may contain dirty user changes. Inspect `git status --short` before editing and avoid reverting work you did not make.

## Build and Verification

Prefer targeted verification first, then broaden only when the change warrants it.

```bash
dotnet build Iridium.slnx --no-restore
```

For frontline-only changes:

```bash
dotnet build frontline/Iridium_frontline.csproj --no-restore
```

If dependencies are missing, restore explicitly before building:

```bash
dotnet restore Iridium.slnx
dotnet build Iridium.slnx --no-restore
```

When changing `.iml` UI files or handler registration code, also audit that every referenced handler and binding still exists.

## Coding Style

- Follow the surrounding C# style and existing project organization.
- Prefer small, named helper methods when they make responsibilities clearer.
- Avoid broad abstractions unless they remove real duplication or match existing architecture.
- Keep comments short and useful; do not restate obvious code.
- Use ASCII for new text unless the file already uses non-ASCII content for a clear reason.

## Dependency Notes

- The build expects local assemblies under `lib/`.
- `Directory.Build.targets` provides fallback hint paths for `Iris.Iml` references from the flat `lib/` directory.
- Do not replace local dependency resolution with package downloads unless requested.

## Review Checklist

Before finishing a change, report:

- Files changed.
- Verification command(s) run and their result.
- Any skipped verification and the reason.
- Any compatibility-sensitive names or bindings that were intentionally preserved.
