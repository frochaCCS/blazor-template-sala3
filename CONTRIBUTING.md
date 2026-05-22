# Contributing

Thank you for contributing to this project! This document outlines the process for submitting changes.

## Branch Model

- All changes must be submitted via pull requests against the `main` branch.
- Feature branches should be named descriptively (e.g., `feature/add-auth`, `fix/login-bug`).

## Commit Messages

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Common types:
- `fix:` — Bug fixes
- `feat:` — New features
- `perf:` — Performance improvements
- `docs:` — Documentation updates
- `test:` — Adding or updating tests
- `refactor:` — Code refactoring without changing behavior
- `chore:` — Dependency updates, tooling changes
- `ci:` — CI/CD pipeline changes

### Example:
```
feat: add ticket assignment functionality

Implement admin UI to assign support tickets to team members.
- Add assignment dropdown to ticket detail page
- Update TicketService with assignment logic
- Add E2E tests for assignment workflow

Closes #42
```

## Pre-commit Checks

Before committing, run these checks locally to ensure your changes pass CI:

```bash
# Build the solution
dotnet build

# Run all unit and integration tests
dotnet test

# Format code according to .editorconfig
dotnet format
```

### Running E2E Tests Locally

Before committing changes that affect the UI:

```powershell
# Install Playwright browsers (one-time setup)
pwsh tests/ITSupportDesk.E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium

# Start the application
dotnet run --project src/ITSupportDesk.Web

# In another terminal, run E2E tests
dotnet test tests/ITSupportDesk.E2ETests/
```

## Updating Lockfiles

When adding a new NuGet package:

1. Add the package reference to the appropriate `.csproj` file:
   ```xml
   <PackageReference Include="MyPackage" Version="1.0.0" />
   ```

2. Restore the solution to update `packages.lock.json`:
   ```bash
   dotnet restore --locked-mode
   ```

3. If lockfile update is needed, regenerate it:
   ```bash
   dotnet restore --force
   ```

4. Commit both the `.csproj` and `packages.lock.json` changes.

## Pull Request Merging Strategy

We use **squash-merge** strategy for PR merges to keep the main branch history clean and linear. When your PR is approved:

1. **GitHub will automatically squash all commits** into a single commit on `main`.
2. The squashed commit message will use the PR title by default.
3. Ensure your **PR title follows Conventional Commit format** so the squashed commit is well-documented:
   ```
   feat: add ticket assignment functionality
   fix: resolve login timeout issue
   docs: update deployment guide
   ```

### Why squash-merge?

- **Clean history**: `main` branch shows one commit per completed feature/fix
- **Atomic changes**: Each PR becomes a single logical unit with a clear message
- **Easier bisecting**: Finding which commit introduced a bug is straightforward
- **Better changelog generation**: Each squashed commit directly corresponds to a changelog entry

### Commit message best practices

Within a PR, you can have multiple commits with any message format during development. Only ensure:
- Your **PR title is in Conventional Commit format** (this becomes the squashed commit message)
- Individual commits in the PR can be rough/work-in-progress; they get squashed anyway
- Before merging, reviewers will verify the PR title is clear and accurate

## Pull Request Checklist

When submitting a PR, please ensure:

- [ ] Code builds cleanly (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Code is formatted (`dotnet format`)
- [ ] **PR title follows Conventional Commits format** (will be used as the squashed commit message)
- [ ] Documentation is updated (if applicable)
- [ ] Screenshots/videos are attached (if UI changes)
- [ ] No breaking changes without discussion

See [PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md) for the standard template.

## Code Style

This project uses [EditorConfig](.editorconfig) to enforce consistent code style. Run `dotnet format` before committing to automatically apply style rules.

## Questions?

Feel free to open an issue or discussion if you have questions or need clarification on the contribution process.
