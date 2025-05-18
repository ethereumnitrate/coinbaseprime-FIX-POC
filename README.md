# Coinbase Prime FIX POC ( C# /.NET 8 Console )

A **minimal** console application that demonstrates how to:

1. Establish a **TLS‑secured** socket to the Coinbase Prime FIX 4.2 gateway  
2. Generate and attach the **HMAC‑SHA‑256** signature required for FIX logon  
3. Stream **Level 2 order‑book** data (or any other channel) for further analysis  

---

## 1  Prerequisites

| Requirement | Details |
|-------------|---------|
| **.NET 8  or later** | `dotnet --version` ⇒ 8.x + |
| **OpenSSL 3.x** | Used once to fetch Coinbase’s leaf certificate |
| **Windows PowerShell (Admin) or MS DOS** | Needed to import the certificate into *Trusted People* |

> **Why the certificate step?**  
> The FIX gateway presents a certificate signed by Coinbase’s own CA.  
> Unless you add that CA (or leaf) to a trusted store, the TLS handshake will fail.

---

## 2  Install the Coinbase Prime certificate (Windows example)

```powershell
# 1) Pull the full cert‑chain from the gateway …
openssl s_client -showcerts -connect fix.prime.coinbase.com:4198 < NUL `
  | openssl x509 -outform PEM > "C:\Certs\fix-prime.coinbase.com.pem"

# 2) Add the leaf cert to the *Trusted People* store (safer than Root) …
certutil -addstore TrustedPeople "C:\Certs\fix-prime.coinbase.com.pem"
