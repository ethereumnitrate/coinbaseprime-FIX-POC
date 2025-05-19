using CoinbasePrimeFIX;
using System.Text.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Net.WebSockets;
using System.Net;

namespace CoinbasePrimeFIX
{
    class Program
    {
        static string Host = "fix.prime.coinbase.com";
        static int Port = 4198;
        static async Task Main(string[] args)
        {
            bool resetAtLogon = true;

            //I stored the Coinbase credentials in an environment system variable.
            //They are stored in json format example 
            //Variable: COINBASE_PRIME_CREDENTIALS
            //Value: {"accessKey":"XXXXXX", "passphrase": "XXXXXXX"...}
            var blob = Environment.GetEnvironmentVariable("COINBASE_PRIME_CREDENTIALS");
            if (string.IsNullOrEmpty(blob))
            {
                Console.Error.WriteLine("ERROR: COINBASE_PRIME_CREDENTIALS not set");
                return;
            }
            var creds = JsonSerializer.Deserialize<Credentials>(blob)
                        ?? throw new Exception("Invalid JSON in COINBASE_PRIME_CREDENTIALS");

            //Uncomment this line if you want to see the signature key and length.
            //Console.WriteLine($">>> signingKey: '{creds.signingKey}' (len={creds.signingKey.Length})");

            char SOH = '\x01';
            string sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff"); //This is exact format for the timestamp
            string msgType = "A";
            string seqNum = "1";  // will be reset if you clear your store or use ResetOnLogon
            string senderCompId = creds.serviceAccountId; // your FIX service account ID (tag 49)
            string targetComp = "COIN";
            string apiKey = creds.accessKey;        // your REST/API key (for prehash & tag 9407)
            string passphrase = creds.passphrase;       // your passphrase (tag 554)
            string secret = creds.signingKey;       // your HMAC secret

            //This is based on the Coinbase Prime Documentation.
            string prehash = $"{sendingTime}{msgType}{seqNum}{apiKey}{targetComp}{passphrase}";
            Console.WriteLine($">>> PREHASH   : '{prehash}'");
            byte[] rawHash;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                rawHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash));
            string signature = Convert.ToBase64String(rawHash);
            Console.WriteLine($">>> SIGNATURE : '{signature}'");

            var bodySb = new StringBuilder();
            bodySb.Append($"35={msgType}{SOH}");
            bodySb.Append($"34={seqNum}{SOH}");
            bodySb.Append($"49={senderCompId}{SOH}");
            bodySb.Append($"56={targetComp}{SOH}");
            bodySb.Append($"52={sendingTime}{SOH}");
            bodySb.Append($"98=0{SOH}");
            bodySb.Append($"108=30{SOH}");
            bodySb.Append($"554={passphrase}{SOH}");
            bodySb.Append($"96={signature}{SOH}");
            bodySb.Append($"9407={apiKey}{SOH}");
            if (resetAtLogon)
                bodySb.Append($"141=Y{SOH}");

            string body = bodySb.ToString();
            int bodyLength = Encoding.ASCII.GetByteCount(body);

            var msgSb = new StringBuilder();
            msgSb.Append($"8=FIX.4.2{SOH}");
            msgSb.Append($"9={bodyLength}{SOH}");
            msgSb.Append(body);

            byte[] msgBytes = Encoding.ASCII.GetBytes(msgSb.ToString());
            int checksum = 0;
            foreach (var b in msgBytes) checksum += b;
            checksum %= 256;
            msgSb.Append($"10={checksum:000}{SOH}");

            string rawLogon = msgSb.ToString();
            Console.WriteLine("\n=== RAW LOGON ===");
            Console.WriteLine(rawLogon.Replace(SOH, '|'));

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(Host, Port);
            using var ssl = new SslStream(tcp.GetStream(), false,
                (sender, cert, chain, errors) => errors == SslPolicyErrors.None);
            await ssl.AuthenticateAsClientAsync(Host, null, SslProtocols.Tls12, false);
            Console.WriteLine("** TLS handshake complete **\n");

            var data = Encoding.ASCII.GetBytes(rawLogon);
            await ssl.WriteAsync(data, 0, data.Length);
            Console.WriteLine(">> Sent Logon, awaiting server reply...\n");

            var buf = new byte[4096];
            int n = await ssl.ReadAsync(buf, 0, buf.Length);
            var resp = Encoding.ASCII.GetString(buf, 0, n);
            Console.WriteLine("<< Server response:");
            Console.WriteLine(resp.Replace(SOH, '|'));


        }
    }
}