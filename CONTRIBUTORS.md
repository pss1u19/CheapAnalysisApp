# Contributors Guide

Thank you for contributing to CheapAnalysisApp.

## Project license
This repository is licensed under **PolyForm Noncommercial 1.0.0**. By contributing, you agree that your contribution is provided under the same license as the repository.

## Contribution terms
By submitting a pull request, patch, commit, issue attachment, or any other contribution to this repository, you confirm that:

- You wrote the contribution yourself, or you have the legal right to submit it.
- You grant the project maintainer the right to use, modify, distribute, and relicense your contribution as part of this project.
- You understand that the project may later be offered under separate commercial terms by the maintainer.
- You are not submitting code, text, assets, or data that you do not have permission to contribute.

## Copyright
- You keep the copyright to your own contribution.
- You grant the maintainer a perpetual, worldwide, non-exclusive, irrevocable license to use, modify, distribute, sublicense, and relicense your contribution as part of this project and future versions of it.

## Contributor License Agreement (CLA)
The terms above are summarized in plain language; the binding version is the **[Contributor License Agreement](CLA.md)**, which every contributor must sign once. In case of any conflict, `CLA.md` governs.

Signing is automated on each pull request:
1. Open your PR as normal.
2. If you have not signed yet, the CLA Assistant bot comments on the PR.
3. Sign by posting a PR comment with exactly: `I have read the CLA Document and I hereby sign the CLA`
4. You only sign once; later PRs are recognized automatically. The maintainer is exempt.

## Practical rules
- Open an issue before large changes.
- Keep pull requests focused and small.
- Follow the existing coding style and formatter setup.
- Add or update tests when behavior changes.
- Never commit secrets, tokens, credentials, or real personal financial data.

## Branching
Branch names follow a `<type>/<slug>` convention. Use lowercase kebab-case for the slug.

| Type | When to use | Example |
|------|-------------|---------|
| `feature/<task-id>-<slug>` | New work tracked in `docs/PROJECT_TRACKER.xlsx` | `feature/t-004-fastendpoints-host` |
| `fix/<slug>` | Bug fix | `fix/healthz-returns-500` |
| `chore/<slug>` | Tooling, dependencies, housekeeping | `chore/bump-dotnet-sdk` |
| `docs/<slug>` | Documentation-only changes | `docs/threat-model` |

Rules:
- Include the task ID (e.g. `t-004`) in `feature/*` branch names so the branch maps back to a row in the project tracker.
- Do not push auto-generated worktree branches (e.g. `claude/<adjective-name-hash>`) as the canonical PR branch — rename to the convention before opening the PR.
- One branch per logical change; rebase or squash on merge to keep history linear.

## Developer certificate statement
By submitting a contribution, you state:

> I have the right to submit this work under the repository license and the contributor terms in this file.

## No commercial rights from contribution
Contributing to this repository does not give you any ownership of the project as a whole, any trademark rights, or any right to commercialize the project outside the permissions granted by the maintainer.
