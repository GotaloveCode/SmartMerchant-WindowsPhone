using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace SmartMerchant
{
    public class Rest
    {
        internal const string ServiceUrlBase = "https://pesalink-demo.masterpiecefusion.com/api/";

        // holds a reference to the logon token...
        public static string Token { get; set; }

        public static bool HasLogonToken
        {
            get
            {
                return !(string.IsNullOrEmpty(Token));
            }
        }


        public static async Task<KeyValuePair<HttpStatusCode, JObject>> PostAsync(string Url, string jsonstr)
        {
            HttpClient client = new HttpClient();

            if (HasLogonToken)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            }
            //no caching
            client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.Now;

            StringContent content = new StringContent(jsonstr, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ServiceUrlBase + Url, content);

            // load it up...
            string outputJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(Url);
            Debug.WriteLine(outputJson);

            JObject output = JObject.Parse(outputJson);

            return new KeyValuePair<HttpStatusCode, JObject>(response.StatusCode, output);
        }

        //Get methods
        public static async Task<KeyValuePair<HttpStatusCode, JsonObject>> GetAsync(string Url, string myparams)
        {
            HttpClient client = new HttpClient();

            if (HasLogonToken)
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            }

            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            //no caching
            client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.Now;

            Url = Url + myparams;

            HttpResponseMessage response = await client.GetAsync(Url);

            

            string outputJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(Url);
            Debug.WriteLine(outputJson);

            JsonObject output = JsonObject.Parse(outputJson);

            return new KeyValuePair<HttpStatusCode, JsonObject>(response.StatusCode, output);


        }




    }
}
