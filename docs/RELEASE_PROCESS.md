# Release Process

This document describes the automated release process for TrackYourDay.

## Overview

TrackYourDay uses an automated release workflow that calculates version numbers based on [Conventional Commits](https://www.conventionalcommits.org/) and [Semantic Versioning](https://semver.org/).

## Automated Releases

### When Releases Happen

Releases are automatically created when:
- Code is pushed to the `main` branch (typically after merging a Pull Request) - **these are always pre-releases**
- The workflow can also be triggered manually to create either pre-release or regular (stable) releases

### Version Calculation

The workflow analyzes the most recent commit message to determine how to bump the version:

#### Conventional Commit Prefixes

| Prefix | Example | Version Bump | Example Transition |
|--------|---------|--------------|-------------------|
| `feat:` | `feat: add new tracker` | Minor | 1.42.0 → 1.43.0 |
| `fix:` | `fix: resolve crash` | Patch | 1.42.0 → 1.42.1 |
| `feat!` | `feat!: redesign UI` | Minor | 1.42.0 → 1.43.0 |
| `fix!` | `fix!: change API` | Minor | 1.42.0 → 1.43.0 |
| `BREAKING CHANGE` | `feat: new feature\n\nBREAKING CHANGE: ...` | Minor | 1.42.0 → 1.43.0 |
| `chore:` | `chore: update deps` | Patch | 1.42.0 → 1.42.1 |
| `docs:` | `docs: update readme` | Patch | 1.42.0 → 1.42.1 |
| `style:` | `style: format code` | Patch | 1.42.0 → 1.42.1 |
| `refactor:` | `refactor: simplify logic` | Patch | 1.42.0 → 1.42.1 |
| `test:` | `test: add unit tests` | Patch | 1.42.0 → 1.42.1 |
| `perf:` | `perf: optimize query` | Patch | 1.42.0 → 1.42.1 |

#### Version Rules

1. **Major version is always 1** - The major version will not change
2. **Minor version bumps** reset the patch version to 0 (e.g., 1.42.5 → 1.43.0)
3. **Patch version bumps** increment the patch number (e.g., 1.42.5 → 1.42.6)
4. **Pre-release versions** include a suffix like `-alpha.1` or `-beta.2`

### How It Works

1. **Checkout**: The workflow checks out the repository with full git history
2. **Fetch Tags**: All existing version tags are retrieved and validated
3. **Parse Latest Version**: The most recent valid version tag is parsed (e.g., `v1.42.2`)
4. **Analyze Commit**: The latest commit message is examined for conventional commit prefixes
5. **Calculate New Version**: Based on the prefix, the appropriate version component is bumped
6. **Determine Release Type**: Automatic releases (push to main) are always pre-releases with `-alpha.x` suffix
7. **Build & Test**: The application is built and all tests are run
8. **Create Release**: A new GitHub release is created with the calculated version

## Manual Release Creation

To create a release manually (pre-release or regular):

1. Go to **Actions** tab in GitHub
2. Select **Publish Release Automated** workflow
3. Click **Run workflow**
4. To create a **pre-release**:
   - Check **"Is this a pre-release version?"**
   - Enter a pre-release tag (e.g., `alpha`, `beta`, `rc`)
5. To create a **regular (stable) release**:
   - Leave **"Is this a pre-release version?"** unchecked
6. Click **Run workflow**

The workflow will:
- Calculate the next version as usual
- For pre-releases: append the pre-release suffix (e.g., `1.43.0-beta.1`)
- For regular releases: use the version without suffix (e.g., `1.43.0`)
- Mark the release appropriately in GitHub

### Pre-release Numbering

If multiple pre-releases are created for the same version, the number is automatically incremented:
- First pre-release: `1.43.0-alpha.1`
- Second pre-release: `1.43.0-alpha.2`
- Third pre-release: `1.43.0-alpha.3`

## Writing Good Commit Messages

To ensure proper version calculation, follow these guidelines:

### Good Examples

```
feat: add Teams meeting integration
fix: resolve UI deadlock when loading Insights
chore: update MudBlazor to version 6.10.0
docs: add installation instructions
```

### Bad Examples

```
Updated some files
Fixed bug
New feature
WIP
```

### Multi-line Commits

For more detailed commits, use the format:

```
feat: add new activity tracker

This adds a new tracker for monitoring system idle time.
It includes:
- Idle time detection
- Configurable timeout
- Activity resumption detection
```

### Breaking Changes

If you're making a breaking change, use one of these formats:

```
feat!: redesign settings UI

BREAKING CHANGE: The settings structure has changed and will reset user preferences.
```

Or:

```
feat: new configuration system

This introduces a new configuration system.

BREAKING CHANGE: Old configuration files are not compatible.
```

## Workflow Files

The automated release process is defined in:
- `.github/workflows/PublishReleaseAutomated.yml` - Automated versioning and release

The manual release process is still available in:
- `.github/workflows/PublishReleaseManually.yml` - Manual version specification

## Troubleshooting

### Version not incrementing correctly

Check that your commit message follows the conventional commit format with the proper prefix.

### Pre-release not working

Ensure you're triggering the workflow manually and have selected the pre-release option.

### Tag already exists

This can happen if the workflow runs multiple times. The workflow will fail if the tag already exists. Delete the tag and release, then re-run the workflow.

## Technical Details

### Version Comparison

The workflow uses semantic version comparison to ensure:
- New versions are always higher than existing versions
- Pre-release versions are properly ordered
- Version format is consistent (`v1.x.y` or `v1.x.y-tag.n`)

### Artifacts

Each release includes:
- A ZIP file containing the built application
- Release notes with installation instructions
- Version tag in git repository

### Build Configuration

- Target Framework: .NET 9.0
- Platform: Windows x64
- Configuration: Release
- Tests: All tests except Integration category are run before release
