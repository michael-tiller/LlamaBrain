# Developer Certificate of Origin

## Version 1.1

By contributing to LlamaBrain, you assert that you have the right to submit the code under the project license (MIT License).

## What is the DCO?

The Developer Certificate of Origin (DCO) is a lightweight way for contributors to certify that they wrote or have the right to submit the code they are contributing to the project. It is used by many open-source projects, including the Linux kernel.

## How to Sign Off

Every commit must include a "Signed-off-by" line in the commit message. This indicates that you agree to the DCO.

### Using Git

To sign off on a commit, use the `-s` or `--signoff` flag:

```bash
git commit -s -m "Your commit message"
```

Or add it manually to your commit message:

```
Your commit message

Signed-off-by: Your Name <your.email@example.com>
```

### Multiple Sign-offs

If you're committing code that includes contributions from others, you can add multiple sign-off lines:

```
Your commit message

Signed-off-by: Your Name <your.email@example.com>
Signed-off-by: Contributor Name <contributor@example.com>
```

## What You're Certifying

By signing off, you certify that:

1. **You have the right to contribute**: The code you're submitting is either:
   - Your own original work, or
   - Code you have the right to submit under the MIT License

2. **You understand the license**: You understand that your contribution will be licensed under the MIT License (see [LICENSE.md](LICENSE.md))

3. **You're not violating anyone's rights**: The contribution doesn't violate any third-party intellectual property rights

## Enforcement

All Pull Requests must have commits with valid "Signed-off-by" lines. Pull Requests without sign-offs will be blocked from merging.

### CI Check

A CI check verifies that all commits in a Pull Request include the "Signed-off-by" line. If any commit is missing the sign-off, the check will fail and the PR cannot be merged.

### Manual Verification

You can verify sign-offs on commits:

```bash
# Check if a commit has a sign-off
git log --show-signature

# Check all commits in a branch
git log --grep="Signed-off-by"
```

## FAQ

### Do I need to sign off on every commit?

Yes. Every commit in your Pull Request must include a "Signed-off-by" line.

### What if I forget to sign off?

You can amend your commit to add the sign-off:

```bash
git commit --amend --signoff
git push --force-with-lease
```

### Can I use a different name or email?

Yes, but it should match your Git configuration or be a name/email you're comfortable being associated with publicly. The sign-off will appear in the project's commit history.

### What if I'm contributing code I didn't write?

You can still sign off if you have the right to submit it under the MIT License. If the code is from another open-source project, ensure it's compatible with MIT and include proper attribution in your commit message.

## More Information

- [Linux Foundation DCO Page](https://wiki.linuxfoundation.org/dco)
- [DCO FAQ](https://developercertificate.org/)

---

**Note**: This DCO requirement applies to all contributions, including code, documentation, and other project materials.
