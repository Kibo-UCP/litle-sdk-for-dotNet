# Tasks: .NET 10 SDK Upgrade

**Input**: Design documents from `specs/001-dotnet10-upgrade/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Test migration tasks are included because the existing NUnit 2 tests must be migrated to NUnit 3 as part of the upgrade. No new test authoring is requested.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `LitleSdkForNet/LitleSdkForNet/` for SDK source, `LitleSdkForNet/LitleSdkForNetTest/` for tests
- All paths relative to repository root

---

## Phase 1: Setup

**Purpose**: Remove obsolete artifacts and clean the workspace before any project file changes

- [X] T001 Delete batch source file `LitleSdkForNet/LitleSdkForNet/LitleBatch.cs` (contains `litleRequest`, `litleFile`, `litleResponse`, `RandomGen`, `litleTime` — 525 lines)
- [X] T002 [P] Delete batch source file `LitleSdkForNet/LitleSdkForNet/LitleBatchRequest.cs` (contains `litleBatchRequest`, `RFRRequest` — 2563 lines)
- [X] T003 [P] Delete batch source file `LitleSdkForNet/LitleSdkForNet/LitleBatchTransactions.cs` (batch-specific transaction types — 1828 lines)
- [X] T004 [P] Delete vendored SSH library `LitleSdkForNet/LitleSdkForNet/lib/Renci.SshNet.dll`
- [X] T005 [P] Delete NUnit 2 runner config `LitleSdkForNet/LitleSdkForNet.nunit`
- [X] T006 [P] Delete user-specific project files `LitleSdkForNet/LitleSdkForNet/LitleSdkForNet.csproj.user` and `LitleSdkForNet/LitleSdkForNetTest/LitleSdkForNetTest.csproj.user`
- [X] T007 [P] Delete vendored Moq directory `LitleSdkForNet/LitleSdkForNet/Service References/`
- [X] T008 [P] Delete NuGet packages folder `LitleSdkForNet/packages/`
- [X] T009 [P] Delete test packages config `LitleSdkForNet/LitleSdkForNetTest/packages.config`
- [X] T010 [P] Delete unused test entry point `LitleSdkForNet/LitleSdkForNetTest/Program.cs`
- [X] T011 [P] Delete batch unit test files: `LitleSdkForNet/LitleSdkForNetTest/Unit/TestBatch.cs`, `LitleSdkForNet/LitleSdkForNetTest/Unit/TestBatchRequest.cs`, `LitleSdkForNet/LitleSdkForNetTest/Unit/TestRFRRequest.cs`
- [X] T012 [P] Delete batch functional test files: `LitleSdkForNet/LitleSdkForNetTest/Functional/TestBatch.cs`, `LitleSdkForNet/LitleSdkForNetTest/Functional/TestBatchStream.cs`

**Checkpoint**: All obsolete files removed. Repository is clean for project file conversion.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Convert project files to SDK-style and establish .NET 10 build. MUST complete before any source code changes.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T013 Replace `LitleSdkForNet/LitleSdkForNet/LitleSdkForNet.csproj` with SDK-style csproj targeting `net10.0`. Set `RootNamespace=Litle.Sdk`, `AssemblyName=LitleSdkForNet`, `SignAssembly=true`, `AssemblyOriginatorKeyFile=dotNetSDKKey.snk`, `GenerateAssemblyInfo=false`. Add PackageReference for `System.Configuration.ConfigurationManager` 9.0.4. Do NOT include `SSH.NET` — SFTP is out of scope.
- [X] T014 [P] Replace `LitleSdkForNet/LitleSdkForNetTest/LitleSdkForNetTest.csproj` with SDK-style csproj targeting `net10.0`. Set `RootNamespace=Litle.Sdk.Test`, `AssemblyName=LitleSdkForDotNetTest`, `GenerateAssemblyInfo=false`, `IsPackable=false`. Add PackageReferences: NUnit 3.14.0, NUnit3TestAdapter 4.5.0, Microsoft.NET.Test.Sdk 17.9.0, Moq 4.20.72, Mozu.Core.JunitTestLogger 2.2616.2, coverlet.collector 6.0.4. Add ProjectReference to SDK csproj.
- [X] T015 Update `LitleSdkForNet/LitleSdkForNet.sln`: change project type GUIDs from `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` to `{9A19103F-16F7-4668-BE54-9A1E7A4F7556}` for both projects. Remove all non-AnyCPU solution platforms (x86, x64, Mixed Platforms). Keep only `Debug|Any CPU` and `Release|Any CPU`.
- [X] T016 Run `dotnet restore` from `LitleSdkForNet/` to verify all NuGet dependencies resolve

**Checkpoint**: `dotnet restore` succeeds. Foundation ready — source code changes can begin.

---

## Phase 3: User Story 1 — Drop-in SDK Binary Replacement (Priority: P1) MVP

**Goal**: The .NET 10 build of the SDK produces identical XML wire output to the .NET 4.5 build for all online transaction types.

**Independent Test**: Run `dotnet build` and verify assembly is named `LitleSdkForNet.dll`. Run unit tests for online transactions and verify XML serialization output matches expected.

### Implementation for User Story 1

- [X] T017 [US1] Remove SFTP/batch transport code from `LitleSdkForNet/LitleSdkForNet/Communications.cs`: delete `using Tamir.SharpSsh.jsch;` import, delete methods `FtpDropOff` (lines 271-371), `FtpPoll` (lines 373-434), `FtpPickUp` (lines 436-489), `SocketStream` (lines 191-263), `GetBestProtocol` (lines 265-269), and struct `SshConnectionInfo` (lines 491-497)
- [X] T018 [US1] In `LitleSdkForNet/LitleSdkForNet/Communications.cs` method `HttpPostCoreAsync`: remove `ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;` (line 103). Add `#pragma warning disable SYSLIB0014` before and `#pragma warning restore SYSLIB0014` after the `WebRequest.Create(uri)` call (line 104). Add configurable connection limit: read `maxConnections` from config dictionary (default "10") and set `request.ServicePoint.ConnectionLimit` accordingly.
- [X] T019 [US1] Clean up `LitleSdkForNet/LitleSdkForNet/XmlSerializer.cs`: remove `DeserializeObjectFromFile` method (lines 26-38) which depends on batch `litleResponse` class. Keep `SerializeObject` method unchanged.
- [X] T020 [US1] Run `dotnet build LitleSdkForNet/LitleSdkForNet.sln` and verify zero errors, zero warnings (except any intentional SYSLIB0014 pragmas). Verify output assembly is `LitleSdkForNet.dll` in `bin/Release/net10.0/`.

**Checkpoint**: SDK builds on .NET 10. Online transaction API compiles. Wire format classes (`LitleCommonTransactions.cs`, `LitleOnlineTransactions.cs`, `LitleOnline.cs`) are untouched.

---

## Phase 4: User Story 2 — Modernized Project Structure (Priority: P2)

**Goal**: Developer can clone, build, and work with the SDK using standard `dotnet` CLI tooling.

**Independent Test**: Fresh clone → `dotnet restore && dotnet build` → zero errors.

### Implementation for User Story 2

- [X] T021 [US2] Add `**/TestResults/` to `.gitignore`
- [X] T022 [US2] Verify `dotnet restore && dotnet build --configuration Release` succeeds from `LitleSdkForNet/` directory with zero errors

**Checkpoint**: Project structure is fully modernized. `dotnet` CLI works end-to-end.

---

## Phase 5: User Story 3 — NUnit 3 Test Suite Migration (Priority: P3)

**Goal**: All online transaction unit tests pass under NUnit 3 on .NET 10.

**Independent Test**: `dotnet test --filter "FullyQualifiedName~Unit"` → all tests pass.

### Implementation for User Story 3

- [X] T023 [US3] Find and replace `[TestFixtureSetUp]` → `[OneTimeSetUp]` in all remaining test files across `LitleSdkForNet/LitleSdkForNetTest/Unit/`, `LitleSdkForNet/LitleSdkForNetTest/Functional/`, and `LitleSdkForNet/LitleSdkForNetTest/Certification/` directories (~48 files after batch test removal)
- [X] T024 [P] [US3] Find and replace `[TestFixtureTearDown]` → `[OneTimeTearDown]` in all test files (if any occurrences exist)
- [X] T025 [P] [US3] In any remaining unit test files with hardcoded Windows paths (e.g., `"C:\\Somewhere\\Over\\"`), replace with platform-neutral `Path.Combine(Path.GetTempPath(), ...)` and add `using System.IO;` if missing. Check non-batch unit test files in `LitleSdkForNet/LitleSdkForNetTest/Unit/` for any Windows path assumptions.
- [X] T026 [US3] Fix any NUnit 2→3 API incompatibilities in test files: check for `Assert.Throws` signature changes, `TestContext` API changes, or other NUnit 3 breaking changes. Fix compilation errors in test project.
- [X] T027 [US3] Fix any Moq 4.2→4.20 API incompatibilities in test files that use mocking. Check `LitleSdkForNet/LitleSdkForNetTest/Unit/TestLitleOnline.cs` and `LitleSdkForNet/LitleSdkForNetTest/Unit/TestCommunications.cs` for mock setup patterns that may need updating.
- [X] T028 [US3] Run `dotnet test LitleSdkForNet/LitleSdkForNetTest/LitleSdkForNetTest.csproj --filter "FullyQualifiedName~Unit"` and verify all online transaction unit tests pass with zero failures

**Checkpoint**: All unit tests pass on .NET 10. Wire compatibility validated through existing serialization assertions in unit tests.

---

## Phase 6: User Story 4 — CI/CD Pipeline Integration (Priority: P4)

**Goal**: Automated Docker build produces NuGet package with unit test results and coverage.

**Independent Test**: `docker build .` succeeds, test results and NuGet package appear in `/buildoutput/`.

### Implementation for User Story 4

- [X] T029 [P] [US4] Create `LitleSdkForNet/Dockerfile` using Kibo `dotnet-10-build-1` base image. Separate COPY for csproj files (Docker layer caching). `dotnet restore` with both nuget.org and Kibo Nexus sources. `dotnet build`, `dotnet test` with `--filter "FullyQualifiedName~Unit"` and `|| true`, `dotnet pack` to `/buildoutput/nugs`. Collect XPlat Code Coverage and JUnit test results via `Mozu.Core.JunitTestLogger`.
- [X] T030 [P] [US4] Create `LitleSdkForNet/Jenkinsfile` using `@Library('kibo-pipeline-shared-lib')_` with `ngProjectPipeline(KIBO_MAJOR_VERSION: 2, SUPPORTS_NUGET: true, FAIL_ON_TEST_FAILURE: true, DOCKERFILE: './Dockerfile')`
- [X] T031 [P] [US4] Create `LitleSdkForNet/.dockerignore` excluding `.git`, `.gitignore`, `**/bin`, `**/obj`, `**/*.user`, `*.md`, `LICENSE`, `CHANGELOG`, `CONTRIBUTORS`, `kit.bat`

**Checkpoint**: CI/CD pipeline is ready. Docker build produces testable artifacts.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [X] T032 Run full `dotnet build --configuration Release` from `LitleSdkForNet/` and verify zero errors, zero unexpected warnings
- [X] T033 Run `dotnet test --filter "FullyQualifiedName~Unit" --configuration Release` and confirm all tests pass
- [X] T034 Run `dotnet pack --configuration Release --output ./nupkg` and verify NuGet package is produced
- [X] T035 Verify assembly name is `LitleSdkForNet.dll` and root namespace is `Litle.Sdk` in build output

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. All tasks parallelizable.
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational phase completion — source code changes to SDK
- **US2 (Phase 4)**: Depends on US1 completion (build must succeed first)
- **US3 (Phase 5)**: Depends on US1 completion (SDK must compile before tests can run)
- **US4 (Phase 6)**: Depends on US3 completion (tests must pass before CI pipeline runs them)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **User Story 2 (P2)**: Depends on US1 (build verification requires US1 code changes)
- **User Story 3 (P3)**: Depends on US1 (tests compile against updated SDK)
- **User Story 4 (P4)**: Depends on US3 (CI runs tests, tests must pass first)

### Within Each User Story

- Source changes before build verification
- Build verification before test migration
- Test migration before CI pipeline

### Parallel Opportunities

- All Phase 1 tasks (T001-T012) can run in parallel — independent file deletions
- T013 and T014 can run in parallel — independent csproj replacements
- T023 and T024 can run in parallel — independent find/replace operations
- T029, T030, T031 can run in parallel — independent new file creation

---

## Parallel Example: Phase 1 (Setup)

```bash
# All deletions can happen simultaneously:
Task: "Delete LitleBatch.cs"
Task: "Delete LitleBatchRequest.cs"
Task: "Delete LitleBatchTransactions.cs"
Task: "Delete vendored SSH library"
Task: "Delete NUnit 2 runner config"
Task: "Delete batch test files"
```

## Parallel Example: Phase 6 (CI/CD)

```bash
# All new files can be created simultaneously:
Task: "Create Dockerfile"
Task: "Create Jenkinsfile"
Task: "Create .dockerignore"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (delete obsolete files)
2. Complete Phase 2: Foundational (convert csproj files)
3. Complete Phase 3: User Story 1 (fix source code, verify build)
4. **STOP and VALIDATE**: Build succeeds, assembly is `LitleSdkForNet.dll`
5. Wire format is unchanged — safe to proceed

### Incremental Delivery

1. Setup + Foundational → Clean project structure
2. Add US1 → SDK builds on .NET 10 (MVP!)
3. Add US2 → Project structure verified
4. Add US3 → Tests pass, wire compatibility confirmed
5. Add US4 → CI/CD pipeline ready
6. Each story adds confidence without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 is the critical path — wire compatibility is non-negotiable
- `LitleCommonTransactions.cs` (9011 lines) and `LitleOnlineTransactions.cs` (654 lines) are NOT modified — these ARE the wire format contract
- Batch code removal (T001-T003, T011-T012) eliminates ~4,900 lines of source and ~5 test files
- Connection limit config key `maxConnections` added in T018 for performance
