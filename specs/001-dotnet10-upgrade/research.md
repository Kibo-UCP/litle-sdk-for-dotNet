# Research: .NET 10 SDK Upgrade

**Date**: 2026-06-01
**Feature**: .NET 10 SDK Upgrade
**Spec**: [spec.md](./spec.md)

## R1: SDK-Style csproj Migration Path

**Decision**: Replace legacy csproj with SDK-style `<Project Sdk="Microsoft.NET.Sdk">` targeting `net10.0`.

**Rationale**: SDK-style csproj is required for .NET 10 targeting. Legacy csproj (ToolsVersion 12.0) cannot target modern .NET. The migration is well-documented by Microsoft and the project structure is simple (single library + test project).

**Alternatives considered**:
- Multi-target (`net10.0;net45`): Rejected — consumer confirmed on .NET 10+
- `dotnet try-convert` tool: Could automate conversion but project is simple enough for manual conversion. The docs/spec.md already provides the exact csproj content.

**Key findings**:
- `GenerateAssemblyInfo=false` required to keep existing `Properties/AssemblyInfo.cs`
- Strong-name signing preserved via `<SignAssembly>true</SignAssembly>` + `<AssemblyOriginatorKeyFile>dotNetSDKKey.snk</AssemblyOriginatorKeyFile>`
- `System.Configuration.ConfigurationManager` NuGet package needed for `ApplicationSettingsBase` support (built-in to .NET Framework, not to .NET 10)

## R2: Batch Code Removal Scope

**Decision**: Remove all batch processing code and SFTP transport. Keep only online transaction API.

**Rationale**: Per clarification, the SDK is used for online API calls only. Batch code represents ~4,900 lines across 3 source files (`LitleBatch.cs`, `LitleBatchRequest.cs`, `LitleBatchTransactions.cs`) plus the SFTP transport in `Communications.cs` (`FtpDropOff`, `FtpPoll`, `FtpPickUp`, `SocketStream`, `GetBestProtocol`, `SshConnectionInfo`).

**Key findings via Serena symbol analysis**:
- `LitleOnline.cs` has ZERO references to batch code — clean separation
- `litleXmlSerializer.DeserializeObjectFromFile` depends on `litleResponse` (batch class) — serializer must be cleaned up
- `Communications.cs` uses `Tamir.SharpSsh.jsch` (JSch port) for SFTP — NOT Renci.SshNet as initially assumed
- Source files to remove: `LitleBatch.cs` (525 lines), `LitleBatchRequest.cs` (2563 lines), `LitleBatchTransactions.cs` (1828 lines)
- Test files to remove: `Unit/TestBatch.cs`, `Unit/TestBatchRequest.cs`, `Unit/TestRFRRequest.cs`, `Functional/TestBatch.cs`, `Functional/TestBatchStream.cs`
- `SSH.NET` NuGet dependency NOT needed (was only for SFTP replacement)

## R3: ServicePointManager Removal and Connection Limit

**Decision**: Remove `ServicePointManager.SecurityProtocol` (TLS is handled automatically by .NET 10). Add configurable connection limit via `ServicePointManager.DefaultConnectionLimit` or `HttpWebRequest.ServicePoint.ConnectionLimit`.

**Rationale**: .NET 10 negotiates TLS 1.2+ by default. The explicit `SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11` setting is unnecessary and `ServicePointManager` is obsolete. However, `HttpWebRequest` still internally respects `ServicePoint.ConnectionLimit` in .NET 10, so we can use it to override the 2-connection default.

**Key findings**:
- Only 1 occurrence of `ServicePointManager.SecurityProtocol` in `HttpPostCoreAsync` (line 103)
- `request.ServicePoint.MaxIdleTime = 8000` and `Expect100Continue = false` at lines 135-136 — keep these
- Connection limit should be configurable via the existing `Dictionary<string,string>` config pattern (e.g., key `"maxConnections"`)

## R4: WebRequest Obsolescence

**Decision**: Keep `HttpWebRequest`/`WebRequest.Create()` with `#pragma warning disable SYSLIB0014`.

**Rationale**: Migrating to `HttpClient` changes connection pooling, timeout semantics, and TLS negotiation behavior. Since wire compatibility is non-negotiable, preserving the exact HTTP transport minimizes risk. The pragma suppression is the approach recommended in docs/spec.md.

**Key findings**:
- 1 occurrence of `WebRequest.Create(uri)` in `HttpPostCoreAsync` (line 104)
- SFTP-related `WebRequest` usage in `SocketStream` method will be removed with batch code

## R5: NUnit 2 → 3 Migration Scope

**Decision**: Rename `[TestFixtureSetUp]` → `[OneTimeSetUp]` and `[TestFixtureTearDown]` → `[OneTimeTearDown]` across all test files. Replace hardcoded Windows paths.

**Rationale**: NUnit 3 renamed these attributes. The change is mechanical (find/replace). Platform-neutral paths needed for macOS CI.

**Key findings via Serena pattern search**:
- 53 occurrences of `[TestFixtureSetUp]` across 53 test files (30 Unit, 19 Functional, 5 Certification)
- 0 occurrences of `[TestFixtureTearDown]` found — only `SetUp` needs migration
- Hardcoded Windows paths in: `TestBatch.cs`, `TestBatchRequest.cs`, `TestRFRRequest.cs` — but these are batch tests being REMOVED, so path fixes only needed if any non-batch unit tests also have Windows paths
- NUnit 2.6.3 → NUnit 3.14.0 + NUnit3TestAdapter 4.5.0 + Microsoft.NET.Test.Sdk 17.9.0
- Moq upgrade: vendored 4.2 DLL → NuGet 4.20.72 (API compatible for existing test code)

## R6: CI/CD Pipeline

**Decision**: Dockerfile uses Kibo `dotnet-10-build-1` base image. Unit tests only in CI. NuGet package output.

**Rationale**: Functional/certification tests require Vantiv sandbox credentials not available in CI. Unit tests are sufficient for build validation.

**Key findings**:
- `--filter "FullyQualifiedName~Unit"` isolates unit tests in CI
- `Mozu.Core.JunitTestLogger` produces Jenkins-compatible test results
- `coverlet.collector` for XPlat code coverage
- `dotnet pack` produces NuGet package
