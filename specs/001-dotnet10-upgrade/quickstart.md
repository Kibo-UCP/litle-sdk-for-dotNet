# Quickstart: .NET 10 SDK Upgrade Validation

## Prerequisites

- .NET 10 SDK installed (`dotnet --version` → 10.x.x)
- Git repository cloned and on `dotnet10/9.14.0` branch

## Build

```bash
cd LitleSdkForNet
dotnet restore
dotnet build --configuration Release
```

Expected output: `Build succeeded. 0 Warning(s) 0 Error(s)`

## Run Unit Tests

```bash
dotnet test --filter "FullyQualifiedName~Unit" --configuration Release
```

Expected: All online transaction unit tests pass.

## Verify Assembly

```bash
ls LitleSdkForNet/bin/Release/net10.0/LitleSdkForNet.dll
```

Assembly must be named `LitleSdkForNet.dll`.

## Wire Compatibility Smoke Test

```csharp
using Litle.Sdk;

var litle = new LitleOnline(new Dictionary<string, string>
{
    { "url", "https://www.testlitle.com/sandbox/communicator/online" },
    { "username", "YOUR_USERNAME" },
    { "password", "YOUR_PASSWORD" },
    { "merchantId", "YOUR_MERCHANT_ID" },
    { "reportGroup", "Default Report Group" }
});

var sale = new sale
{
    orderId = "1",
    amount = 10010,
    orderSource = orderSourceType.ecommerce
};

var response = litle.Sale(sale);
// Response should contain valid transaction result from sandbox
```

## Manual Functional Tests

Requires Vantiv sandbox credentials in `app.config`:

```bash
dotnet test --filter "FullyQualifiedName~Functional" --configuration Release
```

## NuGet Package

```bash
dotnet pack --configuration Release --output ./nupkg
ls ./nupkg/LitleSdkForNet.*.nupkg
```
