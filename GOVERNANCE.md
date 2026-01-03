# LlamaBrain Governance

## BDFL Model

**LlamaBrain** follows a **Benevolent Dictator For Life (BDFL)** governance model. The project founder and primary maintainer, **Michael Tiller**, owns final decisions on all aspects of the project.

### BDFL Authority

The BDFL has final authority over:
- **Architecture Decisions**: Changes to the 9-component architectural pattern
- **Breaking Changes**: API changes that affect backward compatibility
- **Security Policies**: Security-related decisions and vulnerability handling
- **Release Decisions**: Version numbering, release timing, and feature inclusion
- **Project Direction**: Strategic decisions about features, scope, and priorities

### Decision-Making Process

1. **Routine Changes**: Small fixes, documentation updates, and non-breaking improvements can be merged by maintainers after review
2. **Feature Proposals**: Major features should be discussed via GitHub issues or RFCs before implementation
3. **Breaking Changes**: Require BDFL approval and should be documented in CHANGELOG.md
4. **Security Issues**: Must be reported privately (see [SECURITY.md](SECURITY.md)) and require BDFL review

## Maintainer Criteria

Maintainers are trusted contributors who can review and merge pull requests for routine changes.

### Becoming a Maintainer

To become a maintainer, you must:

1. **Demonstrated Understanding**: Show clear understanding of the LlamaBrain architecture and principles
   - Contributed at least 3 merged PRs
   - Demonstrated knowledge of the 9-component pattern
   - Understanding of deterministic state management

2. **Code Quality**: Consistently submit high-quality code
   - Tests pass and coverage is maintained
   - Code follows project standards
   - Documentation is updated appropriately

3. **Community Engagement**: Active and helpful in the community
   - Responsive to issues and PRs
   - Helpful in discussions
   - Respectful and professional

4. **BDFL Approval**: Final approval from the BDFL

### Maintainer Permissions

Maintainers can:
- **Review PRs**: Review and approve pull requests for routine changes
- **Merge Non-Breaking PRs**: Merge PRs that don't change architecture or break APIs
- **Handle Documentation**: Update and merge documentation changes
- **Triage Issues**: Label, categorize, and respond to issues

Maintainers **cannot**:
- Merge breaking changes without BDFL approval
- Change architecture without BDFL approval
- Modify security-sensitive code (see CODEOWNERS)
- Make release decisions

### Maintainer Responsibilities

- **Code Review**: Review PRs thoroughly, ensuring quality and alignment with architecture
- **Issue Triage**: Help categorize and respond to issues
- **Documentation**: Keep documentation up to date
- **Community**: Help answer questions and guide contributors
- **Testing**: Ensure tests pass and coverage is maintained

## Delegation Model

### Routine PRs (Maintainer Authority)

Maintainers can merge:
- Bug fixes (non-security)
- Documentation updates
- Test additions and improvements
- Non-breaking feature additions
- Code refactoring (non-architectural)

### BDFL Approval Required

The following require BDFL approval:
- **Breaking Changes**: API changes, removal of features, major refactoring
- **Architecture Changes**: Modifications to the 9-component pattern
- **Security Changes**: Security-sensitive code modifications
- **Release Decisions**: Version numbering, release timing
- **Governance Changes**: Changes to this governance document

### Critical Directories

Certain directories require BDFL approval for all changes (see [CODEOWNERS](.github/CODEOWNERS)):
- `/LlamaBrain/Source/Core/Validation/` - Validation gate system
- `/LlamaBrain/Source/Core/Inference/` - Inference pipeline
- `/LlamaBrain/Source/Persona/AuthoritativeMemorySystem.cs` - Core memory system
- `/LlamaBrainRuntime/Assets/LlamaBrainRuntime/Runtime/Core/` - Unity core components
- Security documentation files

## RFC Process (For Major Features)

For significant features or architectural changes:

1. **Create RFC Issue**: Open a GitHub issue with `[RFC]` prefix
2. **Discussion Period**: Allow 1-2 weeks for community discussion
3. **BDFL Decision**: BDFL makes final decision based on discussion
4. **Implementation**: If approved, proceed with implementation

RFCs are encouraged for:
- New major features
- Architectural changes
- Breaking API changes
- Significant scope expansions

## Conflict Resolution

If there's disagreement:

1. **Discussion**: Attempt to resolve through discussion in issues/PRs
2. **Maintainer Mediation**: Maintainers can help mediate discussions
3. **BDFL Decision**: BDFL makes final decision if consensus cannot be reached

The BDFL's decision is final, but decisions are made with consideration for:
- Community feedback
- Technical merit
- Project goals and scope
- Long-term maintainability

## Code of Conduct

All contributors, including maintainers and the BDFL, must follow the [Code of Conduct](CODE_OF_CONDUCT.md). Violations will be handled according to the enforcement guidelines.

## Current Maintainers

- **Michael Tiller** (BDFL) - Project founder and primary maintainer

*Maintainer list will be updated as contributors earn maintainer status.*

## Questions?

For questions about governance:
- Open a GitHub issue with the `[governance]` label
- Contact the BDFL via email: [contact@michaeltiller.com](mailto:contact@michaeltiller.com)
- Discuss on [Discord](https://discord.gg/9ruBad4nrN)
