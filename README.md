# TrackYourDay
Another Time Tracking Tool that will Track Your Day by:
* Registering Activities that You did
* Disovering and Measuring Breaks that You have
* Summarizing Your Work Day

Planned:
* Integration with popular Project Management Tools like Jira to allow registering of Worked Time

## Status
![Build and Test Status](https://github.com/skuty/TrackYourDay/actions/workflows/dotnet.yml/badge.svg?event=push)

Newest version available at [Releases](https://github.com/Skuty/TrackYourDay/releases)

## Supported platforms
* Windows 10 and higher

## Installation and Usage
1. Download ZIP archive from [Releases](https://github.com/Skuty/TrackYourDay/releases)
2. Extract to Local Drive
3. Run TrackYourDay executable file

To Run on Windows Startup:
1. Hit WindowsKey+R 
2. Run command: shell:startup
3. File Location Window will Open
4. Create Shortcut to TrackYourDay executsble file
5. Copy Shortcut to File Location

## Architecture

Track Your Day is based on Trackers and Analysers embedded on 3 main levels:
1. The System Level - Events like Application started, Focus On Application, Mouse Moved are discovered here.
2. The Application Level - Events like Teams Meeting Started, GitLab commit done, Jira Activity, User Manuall Tasks are discovered here.
3. The Insights Level - Interpreted, Aggregated, Summarized Events from System and Application Level presented as User Activity

On Top of those Levels outgoing Integrations are based like Logging Time to Jira or Exporting Time Sheets.

## Releases and Versioning

TrackYourDay uses automated versioning based on [Conventional Commits](https://www.conventionalcommits.org/) and [Semantic Versioning](https://semver.org/).

### Automated Release Process

When changes are merged to the `main` branch, the GitHub Actions workflow automatically:
1. Analyzes the commit message for conventional commit prefixes
2. Calculates the next version number
3. Builds and tests the application
4. Creates a new **pre-release** with the appropriate version tag (e.g., `1.43.0`)

**Note:** Automatic releases are always marked as pre-releases. To create a regular (stable) release, manually trigger the workflow without the pre-release option.

### Version Calculation Rules

The major version is always **1**. The minor and patch versions are calculated based on the commit message prefix:

- `feat:` - New feature → **Minor version bump** (e.g., 1.42.0 → 1.43.0)
- `fix:` - Bug fix → **Patch version bump** (e.g., 1.42.0 → 1.42.1)
- `feat!`, `fix!`, or `BREAKING CHANGE` - Breaking change → **Minor version bump** (since major stays at 1)
- `chore:`, `docs:`, `style:`, `refactor:`, `test:`, `perf:` → **Patch version bump**

### Pre-release Versions

- **Automatic releases** (push to main): Always created as pre-releases (marked with pre-release flag in GitHub)
- **Manual releases**: Can be created as either pre-release or regular (stable) release by triggering the workflow manually
- All versions follow the standard semantic versioning format: `1.x.y`

## Contribution

For Feature Requests, Bugs, Questions or any topic, please create an [Issue](https://github.com/Skuty/TrackYourDay/issues/new/choose).  

Due to nature of this project (mostly learning) Pull Requests probably won't be accepted.

## License

This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. 
