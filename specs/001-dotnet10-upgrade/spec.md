# Feature Specification: .NET 10 SDK Upgrade

**Feature Branch**: `dotnet10/9.14.0`
**Created**: 2026-06-01
**Status**: Draft
**Input**: Upgrade Litle/Vantiv eCommerce .NET SDK from .NET Framework 4.5 to .NET 10 while preserving wire-level compatibility and avoiding recertification

## Clarifications

### Session 2026-06-01

- Q: Target framework strategy — net10.0 only or multi-target? → A: net10.0 only — clean break, consumers must also target .NET 10+
- Q: Batch processing delivery scope — must batch SFTP work before merge? → A: SFTP not needed — SDK is for online API calls only
- Q: Batch file processing (non-SFTP) — keep batch XML generation? → A: Batch processing entirely out of scope — only online transaction API matters
- Q: WebRequest deprecation approach — pragma suppression or HttpClient migration? → A: Keep WebRequest with pragma suppression, but add configurable connection limit to remove 2-connection-per-host bottleneck
- Q: Functional/Certification test execution in CI? → A: CI runs unit tests only — functional/certification tests are manual pre-release gates

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Drop-in SDK Binary Replacement (Priority: P1)

A Kibo integration developer replaces the existing `LitleSdkForNet.dll` with the .NET 10 build in their payment processing service. The consuming service must also target .NET 10+. All existing online transaction code compiles and runs without modification. XML requests sent to the Vantiv gateway are identical to those produced by the .NET Framework 4.5 build.

**Why this priority**: This is the entire point of the upgrade. If the wire format changes, the integration must be recertified with Vantiv, which is costly and time-consuming. Every other story depends on this one.

**Independent Test**: Build the upgraded SDK, run the existing unit test suite, and diff the XML serialization output against golden files captured from the current certified build.

**Acceptance Scenarios**:

1. **Given** a .NET 10 build of the SDK, **When** a `sale` transaction is serialized, **Then** the XML output is byte-for-byte identical to the .NET Framework 4.5 output for the same input
2. **Given** a .NET 10 build of the SDK, **When** any of the 15 online transaction types (`Authorize`, `AuthReversal`, `Capture`, `CaptureGivenAuth`, `Credit`, `EcheckCredit`, `EcheckRedeposit`, `EcheckSale`, `EcheckVerification`, `ForceCapture`, `Sale`, `RegisterToken`, `DoVoid`, `EcheckVoid`, `UpdateCardValidationNumOnToken`) are serialized, **Then** the XML output matches the certified .NET 4.5 output
3. **Given** existing consumer code that uses `LitleOnline` or `ILitleOnline` for online transactions, **When** compiled against the .NET 10 SDK, **Then** the code compiles without modification

---

### User Story 2 - Modernized Project Structure (Priority: P2)

A developer clones the repo and builds the SDK using standard `dotnet build` and `dotnet test` commands. The legacy MSBuild csproj files have been replaced with SDK-style csproj format. The `SSH.NET` dependency is removed entirely since SFTP batch transfer is out of scope.

**Why this priority**: The SDK-style csproj is a prerequisite for targeting .NET 10 and enables modern tooling (Docker builds, CI/CD, NuGet packaging). This must work before any other modernization.

**Independent Test**: Run `dotnet restore && dotnet build` from the solution root and verify zero errors.

**Acceptance Scenarios**:

1. **Given** the converted SDK-style csproj files, **When** `dotnet restore` is run, **Then** all NuGet dependencies resolve successfully
2. **Given** the modernized project structure, **When** `dotnet build` is run, **Then** the solution compiles with zero errors and the output assembly is named `LitleSdkForNet.dll`
3. **Given** the removal of batch/SFTP code and vendored DLLs, **When** the SDK is built, **Then** only online transaction functionality is included and no SSH/SFTP dependencies exist

---

### User Story 3 - NUnit 3 Test Suite Migration (Priority: P3)

A developer runs `dotnet test` and all unit tests for online transaction functionality pass. The test project has been migrated from NUnit 2 to NUnit 3 with updated attributes and platform-neutral temp paths. Batch-related tests are removed since batch processing is out of scope.

**Why this priority**: Tests must pass to validate wire compatibility (US1) but the test migration itself is mechanical and lower-risk.

**Independent Test**: Run `dotnet test --filter "FullyQualifiedName~Unit"` and verify all online transaction unit tests pass.

**Acceptance Scenarios**:

1. **Given** unit test files with `[TestFixtureSetUp]` attributes, **When** the NUnit 2→3 migration is applied, **Then** all attributes are renamed to `[OneTimeSetUp]` and tests compile
2. **Given** unit tests with hardcoded Windows paths (`C:\Somewhere\Over\`), **When** the tests run on macOS or Linux, **Then** the tests pass using platform-neutral `Path.Combine(Path.GetTempPath(), ...)` calls
3. **Given** the complete unit test suite, **When** `dotnet test --filter "FullyQualifiedName~Unit"` is run, **Then** all online transaction unit tests pass with zero failures

---

### User Story 4 - CI/CD Pipeline Integration (Priority: P4)

A CI/CD pipeline builds the SDK in a Docker container, runs unit tests only, collects coverage, and produces a NuGet package. Functional and certification tests are manual pre-release gates, not automated in CI. The pipeline integrates with the existing Kibo Jenkins shared library.

**Why this priority**: Required for production builds and deployment but not blocking development work.

**Independent Test**: Build the Docker image locally and verify test results and NuGet package output appear in `/buildoutput/`.

**Acceptance Scenarios**:

1. **Given** the Dockerfile using the `dotnet-10-build-1` base image, **When** `docker build` is run, **Then** the build completes, unit tests run, and a NuGet package is produced
2. **Given** the Jenkinsfile using `ngProjectPipeline`, **When** a CI build is triggered, **Then** build artifacts, unit test results (JUnit XML), and code coverage reports are collected
3. **Given** the CI pipeline, **When** functional or certification tests are needed, **Then** they are run manually with sandbox credentials outside of CI

---

### Edge Cases

- What happens when .NET 10 changes default XML serializer behavior for nullable types? The SDK MUST override defaults to match prior output.
- How does the SDK handle the obsolete `WebRequest` API in .NET 10? The existing implementation is preserved with `#pragma warning disable SYSLIB0014` suppressions.
- What happens when existing `app.config` / `LitleSdkForDotNet.dll.config` configuration files are used? `System.Configuration.ConfigurationManager` NuGet package provides backward compatibility.
- What happens under high concurrency with the default 2-connection-per-host limit inherited from `ServicePointManager`? The SDK MUST expose a configurable connection limit defaulting higher than 2 to avoid this bottleneck.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: SDK MUST build targeting `net10.0` only (no multi-targeting) using SDK-style csproj format
- **FR-002**: SDK MUST produce an assembly named `LitleSdkForNet.dll` with root namespace `Litle.Sdk`
- **FR-003**: All 15 online transaction types on `ILitleOnline` MUST serialize to XML identical to the .NET Framework 4.5 output
- **FR-004**: `Communications` class MUST enforce TLS 1.2+ without using the obsolete `ServicePointManager.SecurityProtocol` — .NET 10 handles TLS negotiation automatically
- **FR-005**: `Communications` MUST expose a configurable connection limit (via the existing `Dictionary<string,string>` config mechanism) to replace the legacy `ServicePointManager.DefaultConnectionLimit` bottleneck of 2 connections per host
- **FR-006**: Configuration via `Dictionary<string,string>` constructor and `.dll.config` file MUST continue to work
- **FR-007**: All NUnit 2 attributes (`[TestFixtureSetUp]`, `[TestFixtureTearDown]`) MUST be migrated to NUnit 3 equivalents (`[OneTimeSetUp]`, `[OneTimeTearDown]`)
- **FR-008**: Hardcoded Windows paths in unit test files MUST be replaced with platform-neutral alternatives
- **FR-009**: Obsolete files (vendored DLLs, NUnit 2 runner config, unused Program.cs, packages/ directory) MUST be removed
- **FR-010**: All batch processing code (`litleRequest`, `litleFile`, `litleResponse`, `FtpDropOff`, `FtpPoll`, `FtpPickUp`, `SocketStream`, `GetBestProtocol`, `SshConnectionInfo`) and the `Tamir.SharpSsh` / `SSH.NET` dependency MUST be removed or excluded from the build
- **FR-011**: Batch-related test files (`TestBatch.cs`, `TestBatchRequest.cs`, `TestBatchStream.cs`, `TestRFRRequest.cs`) MUST be removed or excluded since batch processing is out of scope
- **FR-012**: Dockerfile MUST use Kibo `dotnet-10-build-1` base image, run unit tests only (`--filter "FullyQualifiedName~Unit"`), and produce NuGet packages
- **FR-013**: `GenerateAssemblyInfo` MUST be set to `false` to preserve existing `Properties/AssemblyInfo.cs`
- **FR-014**: The obsolete `WebRequest.Create()` call MUST be preserved with `#pragma warning disable SYSLIB0014` suppression — no migration to `HttpClient`

### Key Entities

- **LitleOnline / ILitleOnline**: Primary API surface for online transactions — 15 transaction types with sync and async variants (30 methods total)
- **Communications**: HTTPS transport layer with TLS enforcement, proxy support, and configurable connection limits
- **litleXmlSerializer**: XML serialization/deserialization engine — the critical path for wire compatibility
- **Settings**: Configuration management via `Litle.Sdk.Properties.Settings`

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero XML serialization differences between the .NET 10 build and the .NET Framework 4.5 build for all 15 supported online transaction types
- **SC-002**: 100% of online transaction unit tests pass after migration
- **SC-003**: SDK builds and packages in under 5 minutes via `dotnet build` and `dotnet pack`
- **SC-004**: Existing consumer code using online transaction APIs compiles against the upgraded SDK without any source changes (consumer must target .NET 10+)
- **SC-005**: Integration operates under existing Vantiv certification without requiring recertification
- **SC-006**: Connection limit is configurable and defaults to a value that avoids the legacy 2-connection bottleneck

## Assumptions

- The consuming Kibo integration service is also upgrading to .NET 10+ (net10.0-only target, no multi-targeting)
- The Kibo `dotnet-10-build-1` Docker base image is available and contains the .NET 10 SDK
- The Vantiv sandbox endpoint (`https://www.testlitle.com/sandbox/communicator/online`) remains available for manual functional/certification testing
- The existing strong-name key file (`dotNetSDKKey.snk`) is present in the repository for assembly signing
- Kibo's internal NuGet feed (`nexus.kibo-dev-ext.com`) hosts the `Mozu.Core.JunitTestLogger` package
- Functional and certification tests require valid Vantiv sandbox credentials and are run manually before release, not in CI
- SFTP batch file transfer is not used by the Kibo integration — the SDK is used for online API calls only
- Batch XML file generation (`litleRequest`/`litleFile`/`litleResponse`) is not used and can be removed
- The `Tamir.SharpSsh` (JSch) library used for SFTP is no longer needed and will be removed along with all batch transport code
