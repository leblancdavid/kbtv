# KBTV Gitflow Development Workflow

This document outlines the gitflow branching strategy for KBTV development.

## Branch Structure

```
main (production)
├── develop (integration)
│   ├── feature/* (new features)
│   ├── bugfix/* (bug fixes)
│   └── release/* (release prep)
└── hotfix/* (production fixes)
```

## Quick Start for Agents

### Starting New Work
```bash
# Start a new feature
git flow feature start my-new-feature

# Or start a bug fix
git flow bugfix start fix-login-issue
```

### Finishing Work
```bash
# Complete your feature
git flow feature finish my-new-feature

# Or complete your bugfix
git flow bugfix finish fix-login-issue
```

## Branch Purposes

### main
- **Purpose**: Production-ready code only
- **When used**: After successful releases
- **Protection**: Should have branch protection rules

### develop
- **Purpose**: Integration branch for latest development
- **When used**: Daily development, feature integration
- **Merges**: Receives completed features and bugfixes

### feature/*
- **Purpose**: Individual feature development
- **Naming**: `feature/descriptive-name` (e.g., `feature/add-voice-models`)
- **Workflow**: Branch from develop, merge back to develop

### bugfix/*
- **Purpose**: Bug fixes for current development
- **Naming**: `bugfix/descriptive-name` (e.g., `bugfix/audio-crash`)
- **Workflow**: Branch from develop, merge back to develop

### release/*
- **Purpose**: Release preparation and testing
- **Naming**: `release/v1.2.0`
- **Workflow**: Branch from develop, merge to main and develop

### hotfix/*
- **Purpose**: Critical production bug fixes
- **Naming**: `hotfix/critical-security-fix`
- **Workflow**: Branch from main, merge to main and develop

## Common Commands

```bash
# Initialize gitflow (if not done)
git flow init -d

# Start new work
git flow feature start <name>
git flow bugfix start <name>
git flow hotfix start <name>

# Finish work
git flow feature finish <name>
git flow bugfix finish <name>
git flow hotfix finish <name>

# List branches
git flow feature list
git flow bugfix list
git flow hotfix list

# Publish branches (for collaboration)
git flow feature publish <name>
git flow feature pull <name>
```

## Release Process

1. **Create release branch**: `git flow release start v1.2.0`
2. **Test and stabilize**: Final testing on release branch
3. **Finish release**: `git flow release finish v1.2.0`
   - Merges to main with version tag
   - Merges back to develop
   - Deletes release branch

## Best Practices

### Commit Messages
- Use imperative mood: "Add feature" not "Added feature"
- Reference issues: "Fix login bug (#123)"
- Keep first line under 50 characters

### Pull Requests
- Create PRs from feature branches to develop
- Include description of changes
- Request review from team members
- Delete merged branches

### Branch Hygiene
- Keep branches short-lived
- Regularly merge develop into long-running feature branches
- Delete local branches after merging

## Troubleshooting

### "Flow command not found"
Install gitflow:
- Windows (Chocolatey): `choco install gitflow-avh`
- Or download from: https://github.com/petervanderdoes/gitflow-avh

### Branch conflicts
```bash
# Abort current flow operation
git flow feature abort
git flow bugfix abort

# Or rebase instead of merge
git flow feature finish -r <name>
```

### Lost work
```bash
# Check reflog
git reflog

# Restore lost commits
git checkout <commit-hash>
```