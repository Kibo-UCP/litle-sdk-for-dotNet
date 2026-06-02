# API Surface Contract: LitleSdkForNet

**Version**: Wire-compatible with XML v9.14
**Constraint**: All public API signatures MUST remain unchanged

## Public Interface: ILitleOnline

```csharp
namespace Litle.Sdk
{
    public interface ILitleOnline
    {
        authorizationResponse Authorize(authorization auth);
        Task<authorizationResponse> AuthorizeAsync(authorization auth);
        authReversalResponse AuthReversal(authReversal reversal);
        Task<authReversalResponse> AuthReversalAsync(authReversal reversal);
        captureResponse Capture(capture capture);
        Task<captureResponse> CaptureAsync(capture capture);
        captureGivenAuthResponse CaptureGivenAuth(captureGivenAuth captureGivenAuth);
        Task<captureGivenAuthResponse> CaptureGivenAuthAsync(captureGivenAuth captureGivenAuth);
        creditResponse Credit(credit credit);
        Task<creditResponse> CreditAsync(credit credit);
        echeckCreditResponse EcheckCredit(echeckCredit echeckCredit);
        Task<echeckCreditResponse> EcheckCreditAsync(echeckCredit echeckCredit);
        echeckRedepositResponse EcheckRedeposit(echeckRedeposit echeckRedeposit);
        Task<echeckRedepositResponse> EcheckRedepositAsync(echeckRedeposit echeckRedeposit);
        echeckSalesResponse EcheckSale(echeckSale echeckSale);
        Task<echeckSalesResponse> EcheckSaleAsync(echeckSale echeckSale);
        echeckVerificationResponse EcheckVerification(echeckVerification echeckVerification);
        Task<echeckVerificationResponse> EcheckVerificationAsync(echeckVerification echeckVerification);
        forceCaptureResponse ForceCapture(forceCapture forceCapture);
        Task<forceCaptureResponse> ForceCaptureAsync(forceCapture forceCapture);
        saleResponse Sale(sale sale);
        Task<saleResponse> SaleAsync(sale sale);
        registerTokenResponse RegisterToken(registerTokenRequestType tokenRequest);
        Task<registerTokenResponse> RegisterTokenAsync(registerTokenRequestType tokenRequest);
        litleOnlineResponseTransactionResponseVoidResponse DoVoid(voidTxn voidTxn);
        Task<litleOnlineResponseTransactionResponseVoidResponse> DoVoidAsync(voidTxn voidTxn);
        litleOnlineResponseTransactionResponseEcheckVoidResponse EcheckVoid(echeckVoid echeckVoid);
        Task<litleOnlineResponseTransactionResponseEcheckVoidResponse> EcheckVoidAsync(echeckVoid echeckVoid);
        updateCardValidationNumOnTokenResponse UpdateCardValidationNumOnToken(updateCardValidationNumOnToken update);
        Task<updateCardValidationNumOnTokenResponse> UpdateCardValidationNumOnTokenAsync(updateCardValidationNumOnToken update);
    }
}
```

## Public Class: LitleOnline

Constructor signatures that MUST be preserved:
```csharp
public LitleOnline()                                    // Uses app.config
public LitleOnline(Dictionary<string, string> config)   // Runtime config
```

Event that MUST be preserved:
```csharp
public event EventHandler<HttpActionEventArgs> HttpAction;
```

## Configuration Keys (Dictionary<string,string>)

Existing keys (MUST remain supported):
- `url` — Gateway endpoint URL
- `username` — Merchant username
- `password` — Merchant password
- `merchantId` — Merchant ID
- `reportGroup` — Default report group
- `logFile` — Log file path
- `neuterAccountNums` — Mask account numbers in logs
- `printxml` — Console debug output
- `proxyHost` — HTTP proxy host
- `proxyPort` — HTTP proxy port

New key (added by this upgrade):
- `maxConnections` — Max concurrent connections per host (default: 10, replaces legacy 2-connection limit)

## Wire Format Contract

XML namespace: `http://www.litle.com/schema`
XML version attribute: `9.14`
Content-Type: `text/xml; charset=UTF-8`
HTTP Method: POST
