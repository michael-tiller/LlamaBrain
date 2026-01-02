# Security Policy

## Supported Versions

We actively support the following versions with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 0.2.x   | :white_check_mark: |
| < 0.2   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via one of the following methods:

### Preferred Method: Private Security Advisory

1. Go to the [Security tab](https://github.com/michael-tiller/LlamaBrain/security/advisories) in this repository
2. Click "Report a vulnerability"
3. Fill out the security advisory form with details about the vulnerability

### Alternative Methods

- **Email**: [Michael](mailto:contact@michaeltiller.com)
- **Discord**: Contact maintainers privately on the [LlamaBrain Discord](https://discord.gg/9ruBad4nrN)

### What to Include

When reporting a vulnerability, please include:

- **Description**: Clear description of the vulnerability
- **Impact**: Potential impact and severity assessment
- **Steps to Reproduce**: Detailed steps to reproduce the issue
- **Affected Versions**: Which versions are affected
- **Suggested Fix**: If you have ideas for a fix (optional but appreciated)
- **Proof of Concept**: If applicable, a minimal example demonstrating the issue

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Resolution**: Depends on severity and complexity

### Security Severity Levels

We use the following severity levels:

- **Critical**: Remote code execution, authentication bypass, data loss
- **High**: Privilege escalation, significant data exposure
- **Medium**: Information disclosure, denial of service
- **Low**: Minor information leakage, best practice violations

### Disclosure Policy

- We will acknowledge receipt of your report within 48 hours
- We will keep you informed of the progress toward resolving the issue
- We will notify you when the vulnerability has been fixed
- We will credit you in the security advisory (unless you prefer to remain anonymous)
- We will coordinate public disclosure after a fix is available

### What We're Looking For

We appreciate reports about:

- Authentication and authorization flaws
- Input validation vulnerabilities
- Path traversal issues
- Injection vulnerabilities (code, command, etc.)
- Information disclosure
- Denial of service vulnerabilities
- Memory corruption issues
- Cryptographic weaknesses
- Configuration issues that lead to security problems

### What We're NOT Looking For

Please do not report:

- Issues that require physical access to the system
- Issues that require social engineering
- Denial of service attacks that require significant resources
- Issues in third-party dependencies (report to the dependency maintainer)
- Issues that require outdated or unsupported configurations
- Best practice violations without security impact

### Security Best Practices

For users of LlamaBrain:

1. **Keep Dependencies Updated**: Regularly update to the latest version
2. **Review Configuration**: Follow security guidelines in [SAFEGUARDS.md](Documentation/SAFEGUARDS.md)
3. **Validate Inputs**: Always validate and sanitize inputs from untrusted sources
4. **Use Rate Limiting**: Configure appropriate rate limits for your use case
5. **Monitor Logs**: Review logs for suspicious activity
6. **Principle of Least Privilege**: Run LlamaBrain with minimal required permissions
7. **Network Security**: If exposing LlamaBrain over a network, use proper authentication and encryption

### Security Measures in LlamaBrain

LlamaBrain implements comprehensive security measures:

- **Input Validation**: All inputs are validated and sanitized
- **Rate Limiting**: Built-in rate limiting (60 requests/minute default)
- **Path Security**: Path traversal prevention in file operations
- **Process Security**: Safe server execution with resource limits
- **Memory Protection**: Bounded memory usage and validation gates
- **Output Validation**: All LLM outputs validated before state mutations

See [SAFEGUARDS.md](Documentation/SAFEGUARDS.md) for detailed security documentation.

### Security Updates

Security updates will be:

- Released as patch versions (e.g., 0.2.1, 0.2.2)
- Documented in [CHANGELOG.md](Documentation/CHANGELOG.md)
- Tagged with security labels in GitHub releases
- Announced in the [Discord](https://discord.gg/9ruBad4nrN) (for critical issues)

### Thank You

We appreciate your help in keeping LlamaBrain secure! Security researchers who responsibly disclose vulnerabilities will be credited in our security advisories.
