# CI/CD Setup Guide

This project uses GitHub Actions with [GameCI](https://game.ci/) for automated Unity builds.

## Overview

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `build.yml` | Version tags (`v*`) | Build Windows + WebGL, create GitHub Release |

## First-Time Setup: Unity License Activation

GameCI requires a Unity license to build. For Personal licenses, follow these steps:

### Step 1: Get Your License File from Unity Hub

1. **Install Unity Hub** on your local machine if you haven't already
2. **Log in to Unity Hub** with the Unity account you want to use for CI
3. **Activate a license**:
   - Go to `Unity Hub` > `Preferences` > `Licenses`
   - Click the `Add` button
   - Select **"Get a free personal license"**
4. **Locate the `.ulf` file** on your machine:
   - **Windows**: `C:\ProgramData\Unity\Unity_lic.ulf`
   - **Mac**: `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Linux**: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

> **Note**: The `ProgramData` folder on Windows is hidden by default. Type the path directly in File Explorer or enable "Show hidden files".

### Step 2: Add Secrets to GitHub

1. Go to your repository on GitHub
2. Navigate to **Settings** > **Secrets and variables** > **Actions**
3. Click **"New repository secret"** and add these three secrets:

| Secret | Value |
|--------|-------|
| `UNITY_LICENSE` | Contents of the `.ulf` file (open in text editor, copy-paste entire file) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

> **Security note**: GameCI does not store your credentials. They are only used during the build to activate Unity.

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
- Make sure you copied the entire file including the XML tags
- Try re-activating in Unity Hub and getting a fresh `.ulf` file

### Can't find the `.ulf` file
- Make sure you clicked **Add** in Unity Hub Licenses and completed activation
- Enable "Show hidden files" in your file explorer
- The file is created after activation, not just after logging in

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
4. Add Steam deployment step to workflow

See [GameCI Steam Deploy docs](https://game.ci/docs/github/deployment/steam) for details.
