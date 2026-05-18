# Versioning

SGG PerfMeter uses calendar build versions.

## Format

The human-readable and package manifest format is:

```text
YYYY.M.D-N
```

Where:

```text
YYYY  release year
M     release month without leading zero
D     release day without leading zero
N     daily build number, starting at 1
```

Example:

```text
2026.5.18-1
```

Do not use leading zeroes in Unity package manifests:

```text
BAD: 2026.05.18-1
BAD: 2026.5.08-1
```

Unity package manifests must stay SemVer-compatible; numeric identifiers with leading zeroes are not valid SemVer.

## Tags

Use the same version for the source release tag:

```text
2026.5.18-1
```

Do not move an existing pushed tag. If a release candidate changes after a tag was pushed, create a new `-N` suffix.

## Package Alignment

Keep these values aligned for each release candidate:

```text
Assets/Scripts/SGG.PerfMeter/package.json version
README.md fixed-version install examples
CHANGELOG.md release entry
docs/release-<version>.md
docs/release-notes-<version>.md
Assets/Scripts/SGG.PerfMeter/CHANGELOG.md release entry
```

The root Unity sample project version stays tied to the installed project editor and is not part of the package calendar version.
