# Contributing Guide

Welcome! This document explains how to contribute to the FIX Order Routing System.

## Table of Contents

1. [Development Setup](#development-setup)
2. [Git Workflow](#git-workflow)
3. [Code Style](#code-style)
4. [Testing](#testing)
5. [Commit Message Format](#commit-message-format)
6. [Pull Request Process](#pull-request-process)
7. [Common Tasks](#common-tasks)

## Development Setup

### Prerequisites

- **.NET 8 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker & Docker Compose** — [Download here](https://www.docker.com/products/docker-desktop)
- **Git** — [Download here](https://git-scm.com/)
- **Visual Studio Code** or **Visual Studio 2022** (optional but recommended)

### Initial Setup

```bash
# 1. Clone the repository
git clone https://github.com/your-org/fix-order-routing-sample.git
cd fix-order-routing-sample

# 2. Restore dependencies
dotnet restore FixOrderRouting.sln

# 3. Start Docker services
docker-compose -f .docker/docker-compose.yml up -d

# 4. Wait for services to be healthy
docker-compose -f .docker/docker-compose.yml ps

# 5. Verify build
dotnet build FixOrderRouting.sln
```

### Verify Your Setup

```bash
# Run tests
dotnet test FixOrderRouting.sln

# Expected output
# Test run successful.
# Total tests: X | Passed: X | Failed: 0
```

## Git Workflow

### Branch Strategy (GitFlow)

```
main ←— develop ←— feature/*
       ↑           ├── feature/order-generator-backend
       └───────────├── feature/order-accumulator
                   └── feature/your-feature
```

### Creating a Feature Branch

```bash
# 1. Sync with latest develop
git checkout develop
git pull origin develop

# 2. Create feature branch
git checkout -b feature/my-feature

# Format: feature/{kebab-case-description}
# Examples:
# - feature/fix-exposure-calculation
# - feature/add-cache-warming
# - feature/improve-error-handling
```

### Pushing Changes

```bash
# 1. Commit changes (see Commit Message Format below)
git add .
git commit -m "feat: add exposure calculation"

# 2. Push to remote
git push origin feature/my-feature

# 3. Create Pull Request on GitHub
```

## Code Style

### C# Conventions

We follow **Microsoft C# Coding Conventions** with these specifics:

#### Naming

```csharp
// Classes, Methods, Properties: PascalCase
public class OrderValidator { }
public void ValidateOrder() { }
public string OrderStatus { get; set; }

// Private fields, local variables: camelCase
private int _retryCount;
private string userName;

// Constants: ALL_CAPS
private const int MAX_ORDER_QUANTITY = 100000;
private const decimal EXPOSURE_LIMIT = 100_000_000m;

// Async methods: end with Async
public async Task<OrderResult> ProcessOrderAsync() { }

// Interfaces: start with I
public interface IOrderRepository { }

// Abbreviations: avoid (OK → Ok)
public class HttpClient { } // Good
public class HTTPClient { } // Avoid
```

#### Formatting

- **Indentation**: 4 spaces (no tabs)
- **Line Length**: 120 characters (soft limit)
- **Braces**: Allman style (opening brace on new line)

```csharp
public class Example
{
    public void MyMethod()
    {
        if (condition)
        {
            DoSomething();
        }
    }
}
```

#### Nullable Reference Types

Enable nullability checking — use `#nullable enable` at top of file:

```csharp
#nullable enable

public class Order
{
    public string Symbol { get; set; } = ""; // Non-nullable (must initialize)
    public string? Reference { get; set; } // Nullable (allowed to be null)
    
    public void Process(string reference)
    {
        if (string.IsNullOrEmpty(reference))
            throw new ArgumentException("Reference required", nameof(reference));
        
        _reference = reference; // OK, reference is not null
    }
}
```

#### Using Statements

Group and sort imports:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using OrderGenerator.Application;
using OrderGenerator.Domain;
```

### Project Structure

Each layer should contain:

```
OrderGenerator.Application/
├── CreateOrder/
│   ├── CreateOrderCommand.cs       (Command)
│   ├── CreateOrderHandler.cs       (Handler)
│   └── CreateOrderValidator.cs     (Validator)
├── GetOrder/
│   ├── GetOrderQuery.cs            (Query)
│   └── GetOrderHandler.cs          (Handler)
├── ApplicationDependencyInjection.cs
└── DependencyInjection.cs
```

### Documentation Comments

Use XML doc comments for public APIs:

```csharp
/// <summary>
/// Creates a new order for the specified symbol and side.
/// </summary>
/// <param name="command">The create order command.</param>
/// <returns>A task that represents the asynchronous operation.</returns>
/// <exception cref="ValidationException">Thrown if command is invalid.</exception>
public async Task<CreateOrderResponse> Handle(CreateOrderCommand command)
{
    // ...
}
```

## Testing

### Unit Tests

**Location**: `tests/OrderGenerator.UnitTests/`, `tests/OrderAccumulator.UnitTests/`

**Test Structure** (AAA pattern):

```csharp
[Fact]
public void ValidateOrder_WithValidSymbol_Succeeds()
{
    // Arrange
    var command = new CreateOrderCommand
    {
        Symbol = "PETR4",
        Side = OrderSide.Buy,
        Quantity = 100,
        Price = 25.50m
    };
    var validator = new CreateOrderValidator();
    
    // Act
    var result = validator.Validate(command);
    
    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
}

[Fact]
public void ValidateOrder_WithInvalidQuantity_Fails()
{
    // Arrange
    var command = new CreateOrderCommand { Quantity = 100_000 }; // > limit
    var validator = new CreateOrderValidator();
    
    // Act
    var result = validator.Validate(command);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
}
```

**Test Naming**: `MethodUnderTest_Scenario_ExpectedOutcome`

### Integration Tests

**Location**: `tests/OrderGenerator.IntegrationTests/`

**Example**:

```csharp
public class CreateOrderIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }

    [Fact]
    public async Task CreateOrder_ViaApi_SavesToDatabase()
    {
        // Arrange
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var order = new { symbol = "PETR4", side = "BUY", quantity = 100, price = 25.50 };
        
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", order);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### Running Tests

```bash
# All tests
dotnet test FixOrderRouting.sln

# Specific project
dotnet test tests/OrderGenerator.UnitTests

# With coverage
dotnet test FixOrderRouting.sln /p:CollectCoverage=true

# Specific test
dotnet test -k "TestName"
```

## Commit Message Format

Follow **Conventional Commits**:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style (formatting, semicolons, etc.)
- **refactor**: Code refactoring without feature change
- **perf**: Performance improvement
- **test**: Adding or updating tests
- **chore**: Build, CI/CD, dependencies
- **ci**: CI/CD configuration changes

### Scope

Relevant module or feature:
- **generator**: OrderGenerator service
- **accumulator**: OrderAccumulator service
- **auth**: Authentication/Authorization
- **infra**: Infrastructure (Docker, database)

### Subject

- Use imperative mood ("add" not "added")
- Don't capitalize first letter
- No period at end
- Limit to 50 characters

### Examples

```bash
git commit -m "feat(generator): add order submission endpoint"
git commit -m "fix(accumulator): correct exposure calculation for negative values"
git commit -m "test(generator): add unit tests for order validator"
git commit -m "docs: update architecture decision for event sourcing"
git commit -m "chore(infra): upgrade PostgreSQL to 15.2"
```

## Pull Request Process

### Before Creating a PR

1. **Sync with develop**
   ```bash
   git fetch origin
   git rebase origin/develop
   ```

2. **Run tests locally**
   ```bash
   dotnet test FixOrderRouting.sln
   ```

3. **Build locally**
   ```bash
   dotnet build FixOrderRouting.sln --configuration Release
   ```

4. **Check code style**
   ```bash
   # Using .editorconfig (automatic in most IDEs)
   # Or manually review for naming, formatting
   ```

### Creating a PR

1. Push your branch: `git push origin feature/my-feature`
2. Go to [GitHub Pull Requests](https://github.com/your-org/fix-order-routing-sample/pulls)
3. Click **New Pull Request**
4. Select:
   - **Base**: `develop`
   - **Compare**: `feature/my-feature`
5. Fill in PR title and description (use template)
6. Click **Create Pull Request**

### PR Template

```markdown
## Description

Brief description of changes.

## Related Issues

Closes #123

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Performance improvement

## Testing

Describe testing performed:
- [ ] Unit tests added
- [ ] Integration tests added
- [ ] Manual testing completed

## Checklist

- [ ] Code follows style guidelines
- [ ] Tests pass locally
- [ ] Documentation updated
- [ ] No breaking changes
- [ ] Commit messages follow convention
```

### PR Review Process

- **2+ approvals** required before merge
- **All CI checks** must pass (build, tests, SonarQube)
- **0 conflicts** with base branch
- **Squash and merge** to develop

### After Merge

Your feature branch is automatically deleted. Celebrate! 🎉

## Common Tasks

### Adding a New Feature to OrderGenerator

1. **Create domain model** (`src/OrderGenerator.Domain/Models/`)
   ```csharp
   public record Order(
       Guid Id,
       string Symbol,
       OrderSide Side,
       int Quantity,
       decimal Price);
   ```

2. **Create command** (`src/OrderGenerator.Application/Features/Orders/CreateOrder/`)
   ```csharp
   public record CreateOrderCommand(
       string Symbol,
       OrderSide Side,
       int Quantity,
       decimal Price) : IRequest<CreateOrderResponse>;
   ```

3. **Create handler** (`CreateOrderHandler.cs`)
   ```csharp
   public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
   {
       public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

4. **Create validator** (`CreateOrderValidator.cs`)
   ```csharp
   public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
   {
       public CreateOrderValidator()
       {
           RuleFor(x => x.Symbol).NotEmpty().Length(4, 5);
           RuleFor(x => x.Quantity).GreaterThan(0).LessThan(100_000);
       }
   }
   ```

5. **Create API endpoint** (`src/OrderGenerator.Api/Controllers/`)
   ```csharp
   [HttpPost("orders")]
   public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
   {
       var result = await _mediator.Send(command);
       return Ok(result);
   }
   ```

6. **Add unit tests**
7. **Add integration tests**
8. **Commit and create PR**

### Updating Database Schema

1. **Create EF Core migration**
   ```bash
   dotnet ef migrations add AddOrderTable \
       --project src/OrderGenerator.Infra \
       --startup-project src/OrderGenerator.Api
   ```

2. **Review migration** (`src/OrderGenerator.Infra/Migrations/`)

3. **Update DbContext** if needed

4. **Test migration**
   ```bash
   dotnet ef database update --project src/OrderGenerator.Infra
   ```

5. **Commit migration files**

### Running Docker Locally

```bash
# Start all services
docker-compose -f .docker/docker-compose.yml up -d

# View logs
docker-compose -f .docker/docker-compose.yml logs -f

# Stop services
docker-compose -f .docker/docker-compose.yml down

# Rebuild images
docker-compose -f .docker/docker-compose.yml build --no-cache
```

### Debugging

```bash
# In VS Code or Visual Studio, set breakpoint and:
dotnet run --project src/OrderGenerator.Api

# Attach debugger to process
```

### Profiling

Use **dotTrace** or **dotMemory** from JetBrains:

```bash
# With profiler
dotnet run --project src/OrderGenerator.Api
```

## Getting Help

- **Questions**: Create [GitHub Discussion](https://github.com/your-org/fix-order-routing-sample/discussions)
- **Bugs**: Open [GitHub Issue](https://github.com/your-org/fix-order-routing-sample/issues)
- **Code Review**: Ask in PR comments
- **Architecture**: See [ARCHITECTURE.md](ARCHITECTURE.md)

---

Thank you for contributing! 🚀
