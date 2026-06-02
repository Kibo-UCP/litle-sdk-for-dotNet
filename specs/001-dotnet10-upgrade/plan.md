# Implementation Plan: .NET 10 SDK Upgrade

**Branch**: `dotnet10/9.14.0` | **Date**: 2026-06-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-dotnet10-upgrade/spec.md`

## Summary

Upgrade the Litle/Vantiv eCommerce .NET SDK from .NET Framework 4.5 to .NET 10, targeting online transaction API only (batch/SFTP removed). Wire-level XML output must remain identical to avoid Vantiv recertification. Project files converted to SDK-style csproj, tests migrated from NUnit 2→3, CI/CD pipeline added.

## Technical Context

**Language/Version**: C# / .NET 10 (upgrading from .NET Framework 4.5)
**Primary Dependencies**: System.Configuration.ConfigurationManager 9.0.4, NUnit 3.14.0, Moq 4.20.72
**Storage**: N/A (stateless SDK)
**Testing**: NUnit 3 + NUnit3TestAdapter 4.5.0 + Microsoft.NET.Test.Sdk 17.9.0
**Target Platform**: .NET 10 (net10.0 only, no multi-targeting)
**Project Type**: Library (payment processing SDK)
**Performance Goals**: Configurable connection limit (default 10, replacing legacy 2-per-host bottleneck)
**Constraints**: Wire-compatible XML output, no API surface changes, no recertification
**Scale/Scope**: ~16K lines source, ~58 test files, 10 source files → reduced to ~6 source files after batch removal

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Wire Compatibility (NON-NEGOTIABLE) | PASS | XML serialization classes (`LitleCommonTransactions.cs`, `LitleOnlineTransactions.cs`) retained as-is. `litleXmlSerializer.SerializeObject` unchanged. No new elements or attributes. |
| II. API Surface Preservation | PASS | `ILitleOnline` interface, `LitleOnline` class, all 30+ public methods retained. Namespace remains `Litle.Sdk`. Constructors preserved. |
| III. XML Schema Fidelity | PASS | `LitleCommonTransactions.cs` (9011 lines of XSD-mapped classes) retained without modification. All `[XmlElement]`/`[XmlAttribute]` annotations preserved. |
| IV. Certification-Safe Testing | PASS | Unit tests validate serialization output. Functional/certification tests available as manual gates. |
| V. Incremental Migration | PASS | Plan structured as independent phases with checkpoints. Each phase produces a buildable/testable state. |

**Post-design re-check**: All principles still pass. Batch code removal does not affect online transaction wire format (confirmed via Serena: `LitleOnline.cs` has zero references to batch code).

## Project Structure

### Documentation (this feature)

```text
specs/001-dotnet10-upgrade/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research output
├── data-model.md        # Entity analysis
├── quickstart.md        # Build/test/validate guide
├── contracts/
│   └── api-surface.md   # Public API contract
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
LitleSdkForNet/
├── LitleSdkForNet.sln           # Solution file (updated GUIDs + platforms)
├── LitleSdkForNet/              # SDK library
│   ├── LitleSdkForNet.csproj    # SDK-style csproj (net10.0)
│   ├── LitleOnline.cs           # Online API entry point (retained)
│   ├── LitleOnlineTransactions.cs  # Online response types (retained)
│   ├── LitleCommonTransactions.cs  # XML model classes (retained, 9011 lines)
│   ├── Communications.cs        # HTTP transport (modified: remove SFTP, add connection limit)
│   ├── XmlSerializer.cs         # Serialization (modified: remove batch deserialization)
│   ├── Settings.cs              # Configuration (retained)
│   ├── LitleOnlineException.cs  # Exception type (retained)
│   └── Properties/
│       ├── AssemblyInfo.cs      # Retained (GenerateAssemblyInfo=false)
│       └── Settings.Designer.cs # Retained
├── LitleSdkForNetTest/          # Test project
│   ├── LitleSdkForNetTest.csproj  # SDK-style csproj (net10.0)
│   ├── Unit/                    # ~27 unit test files (batch tests removed)
│   ├── Functional/              # ~17 functional test files (batch tests removed)
│   └── Certification/           # 5 certification test files (retained)
├── Dockerfile                   # NEW: CI build container
├── Jenkinsfile                  # NEW: CI pipeline
└── .dockerignore                # NEW: Docker build exclusions
```

**Structure Decision**: Single project, online-only. Batch source files (`LitleBatch.cs`, `LitleBatchRequest.cs`, `LitleBatchTransactions.cs`) and SFTP transport code removed. No new directories needed.

## Complexity Tracking

No constitution violations to justify.

| Decision | Why | Alternative Rejected Because |
|----------|-----|------------------------------|
| Remove batch code entirely | Only online API used by Kibo integration | Keeping dead code adds maintenance burden and SFTP dependency complexity |
| Keep WebRequest with pragma | Minimizes wire-behavior risk | HttpClient changes connection pooling and timeout semantics |
| net10.0 only (no multi-target) | Consumer is also on .NET 10+ | Multi-targeting adds build complexity for no benefit |
