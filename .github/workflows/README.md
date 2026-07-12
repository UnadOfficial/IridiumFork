# GitHub Actions Workflow

## Build and Release Workflow

This workflow automatically builds and releases the Iridium mod when changes are pushed to the main branch.

### Features

- **Automatic Build**: Builds the main, Iris.Iml, frontline, and UMM projects with .NET 9
- **Version Detection**: Automatically extracts version information from `Info.json` and `VersionManager.cs`
- **Artifact Packaging**: Creates a zip file with the build output
- **Release Management**: Creates GitHub releases with the built artifacts
- **Duplicate Handling**: If a release with the same name already exists, it appends the commit SHA to the release title

### Trigger Conditions

- Pushes to the `main` branch
- Pull requests to the `main` branch (build only, no release)
- Manual runs from the Actions tab (build only, no release)

### Workflow Steps

1. **Build Job** (Windows environment):
   - Checkout repository
   - Checkout the Iris.Iml submodule
   - Setup .NET 9 and Node.js 20
   - Restore and build the supported projects with `dotnet build`
   - Extract version information
   - Package artifacts as zip file
   - Upload build artifacts

2. **Release Job** (only on push to main):
   - Download build artifacts
   - Check for existing releases with same name
   - Create new release or update existing one
   - Upload zip file as release asset

The MelonLoader project is not built in CI because its referenced MelonLoader
and `lib/v2.9.8` assemblies are not tracked in this repository.

### Version Format

The workflow uses the following version format:
- Release builds: `{Version from Info.json}`
- Non-release builds: `{Version}_{type}{minor}` (e.g., `1.0.5_beta1`)

If a release with the same name exists, the SHA is appended: `{Release Name} ({short-sha})`
