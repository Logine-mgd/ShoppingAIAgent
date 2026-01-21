using System.Text.Json.Serialization;

namespace ShoppingAgent.Data
{
    public class BuyerHistory
    {
        [JsonPropertyName("user_id")]
        
        public string user_id { get; set; }
        
        [JsonPropertyName("history")] 
        public List<Item> history { get; set; } = new List<Item>();

        public BuyerHistory()
        {
        }

        public BuyerHistory(string UserId)
        {
            this.user_id = UserId;
        }
        public BuyerHistory(string UserId, List<Item> purchasedItems)
        {
            this.user_id = UserId;
            this.history = purchasedItems;
        }
    }
}
