using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using twitterXcrypto.util;
using System.Web;

namespace twitterXcrypto.crypto
{
    internal static class Coinmarketcap
    {
        private const string URL = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";
        private const string KEY = "1d64fe33-4ccc-4b26-824f-ab6d4083ffa5";

        public static async Task<Dictionary<string, string>> GetCryptoIdentifiers(int numElements)
        {
            Dictionary<string, string> map = new();

            UriBuilder builder = new(URL);
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["start"] = "1";
            query["limit"] = $"{numElements}";

            KeyValuePair<string, IEnumerable<string>>[] headers =
            {
                new("Accepts", new string[]{"application/json"}),
                new("X-CMC_PRO_API_KEY", new string[]{KEY})
            };

            HttpClient client = new();
            headers.ForEach(header => client.DefaultRequestHeaders.Append(header));

            var response = await client.GetAsync(query.ToString());

            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();



            return map;
        }
    }
}
