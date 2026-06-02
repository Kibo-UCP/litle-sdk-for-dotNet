<!--
  Sync Impact Report
  ===================
  Version change: 0.0.0 → 1.0.0 (MAJOR — initial ratification)
  Added sections:
    - Principle I: Wire Compatibility (NON-NEGOTIABLE)
    - Principle II: API Surface Preservation
    - Principle III: XML Schema Fidelity
    - Principle IV: Certification-Safe Testing
    - Principle V: Incremental Migration
    - Section: Upgrade Constraints
    - Section: Development Workflow
    - Section: Governance
  Removed sections: none (first version)
  Templates requiring updates:
    - .specify/templates/plan-template.md — ✅ no changes needed
      (Constitution Check section is dynamically filled)
    - .specify/templates/spec-template.md — ✅ no changes needed
      (Requirements section accommodates wire-compat FR naturally)
    - .specify/templates/tasks-template.md — ✅ no changes needed
      (Phase structure accommodates constraint-driven tasks)
  Follow-up TODOs: none
-->

# Litle SDK for .NET — Project Constitution

## Core Principles

### I. Wire Compatibility (NON-NEGOTIABLE)

Every XML request and response produced by the upgraded SDK MUST be
byte-for-byte identical to what the current certified SDK produces for
the same logical transaction. "Same over the wire" means:

- Element names, attribute names, and namespace URIs MUST NOT change.
- Element ordering within a parent MUST remain identical.
- Default values, omitted-vs-empty-element behavior, and whitespace
  semantics MUST match the existing serialization output.
- No new required elements or attributes may be introduced unless the
  Vantiv/Worldpay XML schema version (currently 9.14) explicitly
  adds them and the gateway accepts them.

**Rationale**: Vantiv requires recertification for any wire-level
change. Preserving exact wire output lets the Kibo integration
continue operating under its existing certification.

### II. API Surface Preservation

The public C# API (`Litle.Sdk` namespace) MUST remain source-compatible
with existing consumer code:

- All public classes, methods, properties, and enums MUST retain their
  current names and signatures.
- Namespace MUST remain `Litle.Sdk`.
- Constructor overloads (e.g., `LitleOnline()`,
  `LitleOnline(Dictionary)`) MUST continue to work.
- Configuration via `LitleSdkForDotNet.dll.config` MUST remain
  supported alongside any new configuration mechanism.

New APIs MAY be added but MUST NOT alter the behavior of existing
entry points. Deprecation annotations are acceptable; removal is not.

**Rationale**: Downstream integrations compile against the public API.
Breaking it forces code changes and re-testing across all consumers,
which is equivalent to recertification cost.

### III. XML Schema Fidelity

The SDK MUST serialize and deserialize against the Vantiv eCommerce
XML schema version declared on the current branch (9.14):

- All XSD-generated or hand-maintained model classes MUST faithfully
  represent the schema — no fields added, removed, or retyped
  beyond what the XSD defines.
- Serialization attributes (`[XmlElement]`, `[XmlAttribute]`,
  `[XmlEnum]`, ordering hints) MUST produce output that validates
  against the XSD.
- If .NET 10 changes default XML serializer behavior (e.g., nullable
  handling, enum formatting), the SDK MUST override those defaults
  to match prior output.

**Rationale**: The XSD is the contract between the SDK and the Vantiv
gateway. Schema drift causes transaction declines in production.

### IV. Certification-Safe Testing

Every change MUST be verified against wire-compatibility before merge:

- A baseline corpus of XML request/response pairs from the current
  certified SDK MUST be captured and committed as golden files.
- Automated tests MUST compare serialized output of the upgraded SDK
  against these golden files, failing on any diff.
- Integration tests against the Vantiv sandbox
  (`https://www.testlitle.com/sandbox/communicator/online`) MUST
  pass for all supported transaction types.
- Batch file format (`LitleBatch`, `LitleBatchRequest`) MUST produce
  files accepted by the sandbox batch processor.

**Rationale**: Regressions in wire format are invisible at compile
time. Golden-file comparison is the only reliable gate.

### V. Incremental Migration

The .NET Framework 4.5 → .NET 10 upgrade MUST be performed in small,
reversible steps:

- Target framework changes, dependency updates, and API adaptations
  MUST be in separate, independently testable commits.
- At every intermediate commit, the SDK MUST build and the existing
  test suite MUST pass (tests may be temporarily skipped with an
  explicit TODO only if they test batch-stream functionality that is
  being reworked).
- No "big bang" rewrite — each commit SHOULD be deployable as a
  valid SDK build.

**Rationale**: Small steps reduce risk and make bisection possible
when a wire-compatibility regression is detected.

## Upgrade Constraints

The following constraints govern the .NET 10 upgrade specifically:

- **TLS**: The SDK currently disables TLS 1.0 (`Communications.cs`).
  The upgraded SDK MUST continue to enforce TLS 1.2+ and MUST NOT
  fall back to deprecated protocols.
- **Assembly name**: The output assembly MUST remain
  `LitleSdkForNet.dll` so that existing deployments can drop in the
  upgraded binary without rebinding references.
- **NuGet compatibility**: If published as a NuGet package, the
  package ID and root namespace MUST match the current package to
  allow in-place upgrades.
- **No new runtime dependencies** that would prevent the SDK from
  running in environments where the current SDK runs, unless the
  dependency is part of the .NET 10 base class library.

## Development Workflow

- Read a file before editing it.
- Run the full test suite after every change
  (`dotnet test` or equivalent).
- Verify build succeeds before committing.
- Never commit secrets, credentials, or `.env` files.
- Prefer editing existing files over creating new ones.
- Keep commits small and focused on a single concern (framework
  upgrade step, serialization fix, test update, etc.).

## Governance

This constitution supersedes all other development practices for the
Litle SDK .NET 10 upgrade project. Amendments require:

1. A written proposal describing the change and its impact on wire
   compatibility.
2. Approval from the integration owner (the person responsible for
   the Vantiv certification).
3. An updated Sync Impact Report prepended to this file.

Version increments follow semantic versioning:
- **MAJOR**: Principle removed or redefined in a backward-incompatible
  way.
- **MINOR**: New principle or section added, or existing guidance
  materially expanded.
- **PATCH**: Clarifications, wording fixes, non-semantic refinements.

All pull requests MUST include a constitution compliance check
confirming that wire output has not changed.

**Version**: 1.0.0 | **Ratified**: 2026-06-01 | **Last Amended**: 2026-06-01
