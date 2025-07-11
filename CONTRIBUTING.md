# Contributing to BKlug.Extensions.Caching.PostgreSql

Thank you for your interest in contributing to this project! We appreciate all contributions, whether they're fixing bugs, improving documentation, adding new features, or just asking questions.

## Code of Conduct

This project adheres to a Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone** your fork locally
3. **Create a feature branch** from the main branch
4. **Install dependencies** with `dotnet restore`
5. **Make your changes**
6. **Run tests** using `dotnet test`

## Development Environment Setup

### Prerequisites
- .NET 6.0 SDK or later
- PostgreSQL 13+ with pg_cron extension installed
- Visual Studio, Visual Studio Code, or other editor of your choice

### Running Tests
Tests require a PostgreSQL database with pg_cron installed. You can set up a local database or use Docker:

```shell
# Using Docker
docker run -d --name pg-cache-test -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:15

# Install pg_cron in the container
docker exec pg-cache-test bash -c "apt-get update && apt-get install -y postgresql-15-cron"
docker exec pg-cache-test bash -c "echo \"shared_preload_libraries = 'pg_cron'\" >> /var/lib/postgresql/data/postgresql.conf"
docker restart pg-cache-test

# Run the tests
dotnet test
```

## Pull Request Process

1. **Update documentation** if needed
2. **Add tests** for new functionality
3. **Ensure tests pass** locally
4. **Create a PR** against the main branch
5. **Update the CHANGELOG.md** with details of your changes

## Code Style

We follow standard .NET coding conventions:

- Use the `.editorconfig` file included in the repository
- Follow Microsoft's [Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
- Write clear XML documentation for all public APIs
- Maintain high test coverage for new code

## Versioning

We use [Semantic Versioning](https://semver.org/) for releases:
- MAJOR version for incompatible API changes
- MINOR version for backward-compatible functionality additions
- PATCH version for backward-compatible bug fixes

## Licensing

By contributing, you agree that your contributions will be licensed under the project's MIT License.

## Reporting Issues

Please use GitHub Issues to report bugs or request features. For security vulnerabilities, please follow the process outlined in SECURITY.md.

When reporting issues, please include:
- Clear description of the issue
- Steps to reproduce
- Expected and actual behavior
- Environment details (OS, .NET version, PostgreSQL version)
- Any relevant logs or screenshots

## Questions or Need Help?

If you have questions or need help, please:
1. Check existing issues first
2. Open a new issue with your question if none exists
3. Provide as much context as possible

Thank you for contributing to make this project better!
