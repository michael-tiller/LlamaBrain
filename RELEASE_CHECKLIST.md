# Release Checklist

This checklist ensures consistent, high-quality releases for LlamaBrain.

## Pre-Release

### Code Quality
- [ ] All tests passing (`dotnet test`)
- [ ] Code coverage maintained (≥88%)
- [ ] No linter errors or warnings
- [ ] All security checks pass

### Version Management
- [ ] CHANGELOG.md updated with version section
  - [ ] All notable changes documented
  - [ ] Breaking changes clearly marked
  - [ ] Migration notes included (if applicable)
- [ ] Version numbers updated:
  - [ ] `LlamaBrain/LlamaBrain.csproj` - `<Version>` tag
  - [ ] `LlamaBrainRuntime/Assets/LlamaBrainRuntime/package.json` - `version` field
  - [ ] Version consistency verified between files

### Documentation
- [ ] README.md reviewed and updated (if needed)
- [ ] USAGE_GUIDE.md reviewed (if features changed)
- [ ] Breaking changes documented in CHANGELOG.md
- [ ] API documentation generated (if public APIs changed)

### Testing
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Unity PlayMode tests pass (if Unity changes)
- [ ] Manual testing completed for new features
- [ ] RedRoom demo verified

## Release

### Git Operations
- [ ] Create git tag: `v0.3.0` (follow SemVer)
  ```bash
  git tag -a v0.3.0 -m "Release v0.3.0"
  git push origin v0.3.0
  ```
- [ ] Tag triggers CI/CD build automatically
- [ ] Verify CI/CD build succeeds

### Artifacts
- [ ] Verify artifacts generated:
  - [ ] DLL package (`LlamaBrain-{version}.zip`)
  - [ ] PDB symbols (if available)
  - [ ] XML documentation (if available)
- [ ] Download and verify artifacts locally (optional)

### GitHub Release
- [ ] Create GitHub release from tag
- [ ] Attach artifacts to release
- [ ] Release notes populated from CHANGELOG.md
- [ ] Mark as pre-release if `-rc.X` or `-beta.X` version
- [ ] Mark as latest release if stable version

## Post-Release

### Communication
- [ ] Update main branch with release notes link (if needed)
- [ ] Announce on Discord (if applicable)
- [ ] Update any external documentation sites

### Monitoring
- [ ] Monitor for critical issues (48 hours)
- [ ] Watch for bug reports related to release
- [ ] Track download/usage metrics (if available)

### Follow-Up
- [ ] Address any critical issues immediately
- [ ] Plan patch release if needed
- [ ] Update ROADMAP.md with completed features

## Version Numbering (SemVer)

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR** (1.0.0): Breaking changes
- **MINOR** (0.1.0): New features, backward compatible
- **PATCH** (0.0.1): Bug fixes, backward compatible
- **PRE-RELEASE** (0.1.0-rc.1): Release candidates
- **PRE-RELEASE** (0.1.0-beta.1): Beta releases

### Examples
- `0.2.0` → `0.3.0`: New features added
- `0.2.0` → `0.2.1`: Bug fixes only
- `1.0.0` → `2.0.0`: Breaking changes
- `0.3.0-rc.1` → `0.3.0`: Release candidate to stable

## Release Types

### Stable Release
- All pre-release checks complete
- No known critical bugs
- Documentation complete
- Mark as "Latest release" on GitHub

### Pre-Release (RC/Beta)
- Features complete but need testing
- Mark as "Pre-release" on GitHub
- Include `-rc.X` or `-beta.X` in version
- Document known issues

### Hotfix Release
- Critical bug fix for stable release
- Minimal changes (bug fix only)
- Fast-track through checklist
- Patch version increment (e.g., 0.2.0 → 0.2.1)

## Troubleshooting

### CI/CD Build Fails
- Check build logs for errors
- Verify version numbers are consistent
- Ensure all dependencies are available
- Fix issues and re-tag if needed

### Artifacts Missing
- Verify build job completed successfully
- Check artifact upload step in CI/CD
- Manually create artifacts if needed (not recommended)

### Version Mismatch
- Verify all version numbers match
- Check CHANGELOG.md version matches tag
- Ensure package.json and .csproj versions match

## Notes

- **Never force-push tags**: Tags should be immutable
- **Always test locally**: Don't rely solely on CI/CD
- **Document breaking changes**: Users need migration paths
- **Keep releases boring**: Automation reduces errors
