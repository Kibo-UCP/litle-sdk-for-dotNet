# Task Completion Checklist

Before considering a coding task done:

1. Build succeeds: `dotnet build` (or `msbuild` for legacy path)
2. All tests pass: `dotnet test` (Unit + Functional; Certification requires sandbox credentials)
3. Wire-compatibility: verify XML serialization output matches golden files (when available)
4. No new compiler warnings introduced
5. Commit is small and focused on a single concern
