# Litle SDK for .NET — Core

Vantiv eCommerce .NET SDK — C# implementation of the Vantiv (now Worldpay) XML API for card-not-present payment processing.

## Source Layout

```
LitleSdkForNet/
├── LitleSdkForNet/          # SDK library (namespace: Litle.Sdk)
│   ├── LitleOnline.cs       # Online transaction entry point (ILitleOnline)
│   ├── LitleBatch.cs        # Batch processing entry point
│   ├── LitleBatchRequest.cs # Batch request builder
│   ├── LitleBatchTransactions.cs
│   ├── LitleOnlineTransactions.cs
│   ├── LitleCommonTransactions.cs
│   ├── Communications.cs    # HTTPS transport (TLS config, request/response)
│   ├── XmlSerializer.cs     # XML serialization (litleXmlSerializer)
│   ├── Settings.cs          # Configuration management
│   └── LitleOnlineException.cs
├── LitleSdkForNetTest/      # Test project
│   ├── Unit/
│   ├── Functional/
│   └── Certification/
└── LitleSdkForNet.sln
```

## Key Classes

- `LitleOnline` / `ILitleOnline` — main API for online transactions
- `Communications` — HTTP client, TLS enforcement, event hooks
- `litleXmlSerializer` — XML serialization/deserialization
- `litleOnlineRequest` / `litleOnlineResponse` — XML message wrappers

## Active Branch Context

Branch `dotnet10/9.14.0`: upgrading from .NET Framework 4.5 → .NET 10 while maintaining XML v9.14 wire compatibility.

## Cross-References

- Tech stack and build details: `mem:tech_stack`
- Code conventions: `mem:conventions`
- Build/test commands: `mem:suggested_commands`
- Task completion checklist: `mem:task_completion`
