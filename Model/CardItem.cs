using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMerchant
{
    public class CardItem
    {
        [JsonProperty("phone_number")]
        public string Phone { get; set; }
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("card_number")]
        public List<string> CardArray { get; set; }
        [JsonProperty("expiry")]
        public List<string> ExpiryArray { get; set; }
        [JsonProperty("cvc")]
        public List<string> CVVArray { get; set; }
        [JsonProperty("bank")]
        public List<string> BankArray { get; set; }
        [JsonProperty("number_of_cards")]
        public int NumberOfCards { get; set; }

    }
}
