## Description

Please include a summary of the change and which issue is fixed (if applicable).

Fixes #(issue number)

## Type of Change

Please delete options that are not relevant:

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Code refactoring
- [ ] Performance improvement
- [ ] Test addition/update

## Changes Made

- Change 1
- Change 2
- Change 3

## Testing

Please describe the tests you ran to verify your changes:

- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing performed
- [ ] Code coverage maintained or improved

### Test Results

```
Paste test results here if applicable
```

## Code Quality Checklist

- [ ] My code follows the project's code style guidelines
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have updated the documentation accordingly
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
- [ ] Any dependent changes have been merged and published

## Architecture Compliance

- [ ] Changes align with the 9-component architectural pattern (see [ARCHITECTURE.md](../Documentation/ARCHITECTURE.md))
- [ ] No direct LLM access to game state
- [ ] All outputs validated before state mutations
- [ ] Authority hierarchy respected (canonical > world state > episodic > beliefs)

## Security Considerations

- [ ] I have reviewed [SAFEGUARDS.md](../Documentation/SAFEGUARDS.md)
- [ ] Input validation added where needed
- [ ] Path security maintained (using PathUtils)
- [ ] Rate limiting respected
- [ ] No sensitive information exposed

## Documentation

- [ ] Code has XML documentation comments (for public APIs)
- [ ] [ARCHITECTURE.md](../Documentation/ARCHITECTURE.md) updated (if architectural changes)
- [ ] [USAGE_GUIDE.md](../Documentation/USAGE_GUIDE.md) updated (if user-facing changes)
- [ ] [STATUS.md](../Documentation/STATUS.md) or [ROADMAP.md](../Documentation/ROADMAP.md) updated (if feature changes)
- [ ] README.md updated (if needed)

## Breaking Changes

If this PR includes breaking changes, please describe them and provide migration instructions:

## Screenshots (if applicable)

Add screenshots to help explain your changes.

## Additional Notes

Any additional information that reviewers should know.

## Related Issues

- Related to #(issue number)
- Closes #(issue number)
- Part of [ROADMAP.md](../Documentation/ROADMAP.md) Feature #X
