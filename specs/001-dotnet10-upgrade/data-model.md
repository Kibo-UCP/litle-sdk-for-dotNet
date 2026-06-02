# Data Model: .NET 10 SDK Upgrade

**Date**: 2026-06-01

## Entities Retained (Online Transaction API)

### LitleOnline (Class)
- **Purpose**: Primary API entry point for online transactions
- **Interface**: `ILitleOnline`
- **Fields**: `config` (Dictionary<string,string>), `communication` (Communications)
- **Constructors**: `LitleOnline()`, `LitleOnline(Dictionary<string,string>)`
- **Transaction methods**: 30 methods (15 sync + 15 async) covering Authorize, AuthReversal, Capture, CaptureGivenAuth, Credit, EcheckCredit, EcheckRedeposit, EcheckSale, EcheckVerification, ForceCapture, Sale, RegisterToken, DoVoid, EcheckVoid, UpdateCardValidationNumOnToken
- **Additional methods**: CancelSubscription, UpdateSubscription, Activate, Deactivate, Load, Unload, BalanceInquiry, CreatePlan, UpdatePlan, RefundReversal, DepositReversal, ActivateReversal, DeactivateReversal, LoadReversal, UnloadReversal, queryTransaction, FraudCheck
- **Internal methods**: createLitleOnlineRequest, sendToLitle, sendToLitleAsync, SendRequest<T>, SendRequestAsync<T>, CreateRequest, DeserializeResponse, SerializeObject, DeserializeObject, FillInReportGroup

### Communications (Class)
- **Purpose**: HTTPS transport with TLS enforcement
- **Methods retained**: HttpPost, HttpPostAsync, HttpPostCoreAsync, OnHttpAction, ValidateServerCertificate, NeuterXml, Log, IsProxyOn
- **Methods REMOVED**: FtpDropOff, FtpPoll, FtpPickUp, SocketStream, GetBestProtocol
- **Struct REMOVED**: SshConnectionInfo
- **New config key**: `maxConnections` (int, default: 10)

### litleXmlSerializer (Class)
- **Purpose**: XML serialization/deserialization
- **Methods retained**: SerializeObject
- **Methods REMOVED**: DeserializeObjectFromFile (depends on batch `litleResponse`)

### litleOnlineRequest / litleOnlineResponse (Classes)
- **Purpose**: XML message wrappers for online transactions
- **Status**: Retained as-is

### LitleCommonTransactions (File: 9011 lines)
- **Purpose**: All XML-serializable transaction type classes, enums, complex types
- **Status**: Retained as-is — this is the wire-format contract

### LitleOnlineTransactions (File: 654 lines)
- **Purpose**: Online-specific transaction response types (voidTxn, echeckVoid, depositReversal, etc.)
- **Status**: Retained as-is

### Settings (Class in Litle.Sdk.Properties)
- **Purpose**: Configuration via app settings
- **Status**: Retained — needs `System.Configuration.ConfigurationManager` NuGet

### LitleOnlineException (Class)
- **Purpose**: Exception type for API errors
- **Status**: Retained as-is

## Entities Removed (Batch Processing)

| Entity | File | Lines | Reason |
|--------|------|-------|--------|
| `litleRequest` | LitleBatch.cs | 525 | Batch orchestration |
| `litleFile` | LitleBatch.cs | (within) | Batch file I/O |
| `litleResponse` | LitleBatch.cs | (within) | Batch response parsing |
| `RandomGen` | LitleBatch.cs | (within) | Batch file naming |
| `litleTime` | LitleBatch.cs | (within) | Batch timestamps |
| `litleBatchRequest` | LitleBatchRequest.cs | 2563 | Batch request builder |
| `RFRRequest` | LitleBatchRequest.cs | (within) | Batch retrieval request |
| All batch transaction types | LitleBatchTransactions.cs | 1828 | Batch-specific types |

## Dependency Changes

| Dependency | Action | Reason |
|------------|--------|--------|
| `Tamir.SharpSsh` (JSch) | REMOVE | Only used by SFTP batch transport |
| `SSH.NET` | NOT NEEDED | Was planned for SFTP replacement — no SFTP needed |
| `System.Configuration.ConfigurationManager` 9.0.4 | ADD | `ApplicationSettingsBase` support in .NET 10 |
| NUnit 3.14.0 | UPGRADE | NUnit 2.6.3 → 3.14.0 |
| NUnit3TestAdapter 4.5.0 | ADD | Replaces NUnitTestAdapter.WithFramework 2.0.0 |
| Microsoft.NET.Test.Sdk 17.9.0 | ADD | Required for test discovery in .NET 10 |
| Moq 4.20.72 | UPGRADE | Vendored 4.2 DLL → NuGet package |
| Mozu.Core.JunitTestLogger 2.2616.2 | ADD | Jenkins JUnit XML reporting |
| coverlet.collector 6.0.4 | ADD | XPlat code coverage |
