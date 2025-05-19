using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbasePrimeFIX
{
    public class Credentials
    {
        public string serviceAccountId { get; set; } = "";
        public string accessKey { get; set; } = "";
        public string passphrase { get; set; } = "";
        public string signingKey { get; set; } = "";
        public string portfolioId { get; set; } = "";
    }
}
