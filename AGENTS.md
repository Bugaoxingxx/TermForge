# C# + NativeAOT + WPF Agent Rules

## Project Context
You are an expert C# developer working with NativeAOT runtime and WPF. This project is a Library.

## Code Style & Structure
### C# Defaults
- Follow .NET naming: `PascalCase` for public members and types, `camelCase` for parameters and locals, `_camelCase` for private fields.
- Use C# 12+ features: primary constructors, collection expressions, pattern matching.
- Use `var` for local variables when the type is obvious from the right side — use explicit types when it improves readability.
- Use file-scoped namespaces (`namespace MyApp;`) to reduce nesting — available in C# 10+.
- Use `record` types for immutable data. Use `readonly` on structs and fields that shouldn't change.
- Prefer `async`/`await` for all I/O operations — never block with `.Result` or `.Wait()`.
- Use nullable reference types (`#nullable enable`). Annotate nullability explicitly.
- Use pattern matching (`is`, `switch` expressions) over type casting and `if`/`else` chains.
- Prefer LINQ methods (`.Where()`, `.Select()`, `.Any()`) over manual loops for querying collections.

### NativeAOT Patterns
- Enable with `<PublishAot>true</PublishAot>` in csproj for ahead-of-time compilation.
- Replace reflection with source generators — use `[JsonSerializable]` for System.Text.Json serialization.
- Avoid `dynamic` keyword and `Assembly.Load()` — they are not supported in NativeAOT.
- Use `DynamicallyAccessedMembersAttribute` to annotate unavoidable reflection usage.
- Use minimal APIs instead of controllers — they avoid the reflection-heavy MVC pipeline.
- Use `IServiceCollection` dependency injection over dynamic object creation.
- Prefer compile-time configuration binding with source generators over reflection-based binding.
- Use `[LoggerMessage]` source generator for high-performance structured logging.
- Avoid `Enum.Parse<T>` in hot paths — use switch expressions or source-generated parsers.

### .NET Conventions
- Use PascalCase for classes, methods, properties, and public members.
- Use camelCase for local variables and private fields (prefix private fields with '_' where conventional).
- Apply async/await for all I/O-bound operations.
- Write clear, descriptive commit messages in English.
- Prefer LINQ and lambda expressions for collection and data manipulation.
- Organize code in layered structure: Domain (entities/interfaces), Application (services/DTOs), Infrastructure (repositories/external), Api/Presentation.
- Employ file-scoped namespaces (`namespace X;`).
- Prefer `var` when type is apparent from context.

### OOP Design Patterns
- Follow SOLID principles for class design.
- Prefer composition over inheritance; use interfaces/protocols for abstraction.
- Encapsulate state within objects; expose behavior through well-defined methods.

## WPF
- Implement `INotifyPropertyChanged` using `[CallerMemberName]` so property setters notify the UI without hardcoding string names — hardcoded strings break silently on rename: `protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));`
- Bind buttons and triggers to `ICommand` properties (use `RelayCommand` from CommunityToolkit.Mvvm) rather than code-behind event handlers — keeps all logic in the ViewModel and testable without a UI: `public ICommand SaveCommand { get; } = new RelayCommand(ExecuteSave, CanSave);`
- Use `ObservableCollection<T>` for all list properties displayed by the UI — plain `List<T>` does not implement `INotifyCollectionChanged`, so adding or removing items never updates bound controls at runtime.
- Never access or mutate UI elements from a background thread — use `await Task.Run(...)` from an `async` method to run work off-thread, then update ViewModel properties after the `await`; WPF's synchronization context marshals the continuation back automatically.
- Define reusable styles, brushes, and data templates in `ResourceDictionary` files merged into `App.xaml` — use `x:Key` for targeted application and `TargetType` without a key for implicit type-wide application; never duplicate `Style` definitions inline across multiple `UserControl` files.
- Use `CollectionViewSource` in XAML or `ICollectionView` in the ViewModel for sorting and filtering — never mutate the backing `ObservableCollection` for display concerns, as that couples data to presentation.

## Architecture
### Library Architecture
- Design a minimal, intuitive public API. Every exported symbol is a commitment — keep the surface area small.
- Follow semantic versioning strictly: breaking changes = major, new features = minor, bug fixes = patch.
- Write comprehensive documentation: README with quick start, API reference, migration guides between major versions.
- Ship both ESM and CJS (for JS/TS) or the idiomatic package format for your language. Support tree-shaking.
- Use the facade pattern: expose a clean public API that hides internal complexity. Internal modules should not be importable.
- Deprecate before removing. Mark APIs as deprecated for at least one major version before removal. Include migration path in deprecation message.
- Write examples for every public function. Examples serve as both documentation and regression tests.
- Minimize dependencies. Every dependency is a liability — it can break, have vulnerabilities, or conflict with user deps.
- Version your error types. Users may match on error kinds, so changing error variants is a breaking change.
- Support both sync and async patterns where applicable. Do not force async on users who do not need it.

## Performance
### Performance Guidelines
- Profile before optimizing — measure, don't guess. Premature optimization wastes time and adds complexity.
- Optimize the critical path first. 90% of performance comes from 10% of the code.
- Cache expensive computations and database queries — use appropriate TTLs and invalidation strategies based on data freshness requirements.
- Cache expensive computations and API calls. Invalidate caches explicitly — stale data is a bug.
- Use lazy loading for non-critical resources and code paths.
- Debounce user-input-driven operations (search, resize, scroll).
- Prefer pagination or virtual scrolling for large data sets — never render 10,000 DOM nodes.

## Error Handling
### Mixed
- Combine exceptions for unexpected failures with Result/Either types for expected business errors.
- Use exceptions for infrastructure failures (network, I/O, OOM) and result types for domain validation errors.
- Never swallow exceptions silently — always log or propagate with context.
- Use exceptions for truly exceptional conditions (infrastructure failures, programming errors) and Result/Either types for expected business-logic failures (validation, not-found, permission denied).
- Wrap third-party library exceptions at module boundaries into your own domain-specific error types.
- Always attach context (operation name, input values, timestamps) when re-throwing or wrapping errors.
- Use a centralized error handler for cross-cutting concerns (logging, monitoring, user-facing messages).
- Prefer typed error enums or union types over generic error strings for pattern matching and exhaustiveness checks.
- Never use exceptions for control flow — reserve them for truly unexpected states.

## Testing
### C# Testing
- Follow the naming convention: `MethodName_Scenario_ExpectedBehavior`.
- Use the Arrange-Act-Assert pattern. One assertion concept per test method.
- Use the `[Fact]` attribute for single tests and `[Theory]` with `[InlineData]` for parameterized tests in xUnit.
- Use shared fixtures for expensive setup (database, HTTP client) across tests in a class.
- Mock interfaces for dependency isolation — prefer mocking libraries with clean, fluent syntax.
- Use parameterized tests for data-driven scenarios with multiple input combinations.
- Use `Verify()` on mocks to assert that expected interactions occurred.

### xUnit
- Use `[Fact]` for single test cases and `[Theory]` with `[InlineData]` for parameterized tests. Use `Assert.Equal()`, `Assert.Throws<T>()`, and `Assert.Contains()`. One assertion focus per test method.
- Use `[ClassFixture<T>]` for shared expensive setup across tests in a class. Use `[CollectionFixture<T>]` for sharing across multiple test classes. Use `IAsyncLifetime` for async setup/teardown instead of constructor/Dispose. Mock dependencies with Moq or NSubstitute: `Mock<IService>().Setup(x => x.Method()).Returns(value)`.

### Integration Testing
- Write integration tests for API endpoints, database operations, and cross-module interactions.
- Run integration tests after implementation to verify components work together correctly.
- Use a real (or realistic) test database — don't mock everything in integration tests.
- Test API routes end-to-end: send a request, verify the response status, body, and side effects (DB writes, events).
- Use test databases with migrations applied — seed minimal data in `beforeEach`, clean up in `afterEach`.
- Test service-to-service interactions: verify that module A correctly calls module B with expected inputs.
- Integration tests are slower than unit tests — run them in CI and before merging, not necessarily on every save.
- Test authentication and authorization flows as integration tests — they span multiple layers.

### Test Coverage & CI
- Run the full test suite in CI on every push and pull request — never merge with failing tests.
- Set coverage thresholds for business-critical code (80%+ for core logic).
- Always run tests locally before pushing — CI is a safety net, not the first line of defense.
- Configure CI to run unit tests first (fast feedback), then integration, then E2E (test pyramid).
- Fail the build when coverage drops below the threshold — prevent gradual test debt accumulation.
- Track coverage trends over time — a declining coverage metric signals a process problem.
- Use test result caching and parallelization to keep CI feedback under 10 minutes.
- Require all tests to pass before merging PRs — no exceptions for "known flaky" tests, fix them instead.

## Libraries & Tools
### Serilog
- Use structured log templates with named properties: `Log.Information("Order {OrderId} placed by {UserId}", orderId, userId)` — never use string interpolation (`$""`). Configure sinks in `Program.cs` with `WriteTo.Console()` and `WriteTo.File()`.
- Use `LogContext.PushProperty()` for correlation IDs and request-scoped data. Use enrichers (`Enrich.FromLogContext()`, `Enrich.WithMachineName()`) for automatic context. Set minimum level per sink: verbose to file, warning to console. Use `Serilog.AspNetCore` with `UseSerilogRequestLogging()` for HTTP request logs. Implement `ILogger<T>` injection via Microsoft DI integration.
