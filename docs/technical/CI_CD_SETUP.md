# CI/CD Setup Guide

# CI/CD Setup (Not Currently Implemented)

This document describes build automation setup for Godot projects. CI/CD is not currently implemented for KBTV.

## Manual Export Process

For now, builds are created manually using Godot's export system:

### Windows Build
```bash
godot --export "Windows Desktop" --output "builds/KBTV_Windows.exe" project.godot
```

### Web Export
```bash
godot --export "HTML5" --output "builds/web/" project.godot
```

### Export Presets
Configure export presets in Godot Editor under **Project > Export**.

## Future CI/CD Setup

When implemented, this will use GitHub Actions with Godot's command-line export functionality:

```yaml
# Example GitHub Actions workflow
name: Export Game
on: [push]
jobs:
  export-windows:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - uses: actions/setup-dotnet@v1
    - uses: mirzaturk/godot-actions@v1
      with:
        version: 4.2.1
    - run: godot --export "Windows Desktop" --output "builds/KBTV_Windows.exe"
    - uses: actions/upload-artifact@v2
      with:
        name: windows-build
        path: builds/
```

## Creating a Release

1. Tag your commit with a version:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```

2. The workflow automatically:
   - Builds Windows (.exe) and WebGL versions
   - Creates a GitHub Release with both zip files
   - Generates release notes from commits

### Version Tag Format

- `v1.0.0` - Full release
- `v0.1.0-alpha` - Pre-release (marked as prerelease on GitHub)
- `v0.1.0-beta.1` - Pre-release with iteration

## Manual Builds

You can also trigger builds manually:

1. Go to **Actions** > **Build and Release**
2. Click **"Run workflow"**
3. Select branch and run

Note: Manual runs without a tag won't create a GitHub Release.

## Local Builds

Use Godot Editor:
- **Project > Export** - Configure export presets and export manually
- Use command-line as shown in Manual Export Process above

## Future: Steam Deployment

When ready for Steam:
1. Create Steamworks developer account ($100)
2. Create app and get App ID
3. Add `STEAM_USERNAME` and `STEAM_CONFIG_VFD` secrets
4. Add Steam deployment step to workflow

See [GameCI Steam Deploy docs](https://game.ci/docs/github/deployment/steam) for details.
