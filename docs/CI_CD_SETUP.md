# CI/CD Setup Guide

This project uses GitHub Actions with [GameCI](https://game.ci/) for automated Unity builds.

## Overview

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build.yml` | Version tags (`v*`) | Build Windows + WebGL, create GitHub Release |
| `activation.yml` | Manual | Generate Unity license activation file |

## First-Time Setup: Unity License Activation

GameCI requires a Unity license to build. For Personal licenses, follow these steps:

### Step 1: Generate Activation File

1. Go to **Actions** tab in your GitHub repository
2. Select **"Acquire Unity License"** workflow
3. Click **"Run workflow"**
4. Download the `Unity_v6000.0.28f1.alf` artifact when complete

### Step 2: Activate License on Unity Website

1. Go to [license.unity3d.com/manual](https://license.unity3d.com/manual)
2. Upload the `.alf` file
3. Select "Unity Personal" license type
4. Download the resulting `.ulf` license file

### Step 3: Add Secrets to GitHub

Go to **Settings > Secrets and variables > Actions** and add:

| Secret | Value |
|--------|-------|
| `UNITY_LICENSE` | Contents of the `.ulf` file (copy-paste entire file) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

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

From Unity Editor:
- **Build > Build Windows** - Outputs to `Build/Windows/KBTV.exe`
- **Build > Build WebGL** - Outputs to `Build/WebGL/`
- **Build > Build All** - Builds both platforms

## Troubleshooting

### "License not found" error
- Verify `UNITY_LICENSE` secret contains the full `.ulf` file contents
- Re-run the activation workflow if the license expired

### Build fails on first run
- The Library cache is empty on first run, so builds take longer
- Subsequent builds use cached Library folder

### WebGL build fails
- Ensure WebGL Build Support is installed in Unity
- Check that no unsupported APIs are used (e.g., System.IO file access)

## Future: Steam Deployment

When ready for Steam:
1. Create Steamworks developer account ($100)
2. Create app and get App ID
3. Add `STEAM_USERNAME` and `STEAM_CONFIG_VFD` secrets
4. Uncomment Steam deployment step in workflow

See [GameCI Steam Deploy docs](https://game.ci/docs/github/deployment/steam) for details.
