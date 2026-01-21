using System.Text.Json.Serialization;

namespace ShoppingAgent.Data
{
    public class Item
    {
        [JsonPropertyName("product")] 
        public string Product { get; set; }
        [JsonPropertyName("category")]
        public string Category { get; set; }
        [JsonPropertyName("price")]
        public int Price { get; set; }

        public Item() { }

        public Item(string product, string category, int price)
        {
            this.Product = product;
            this.Category = category;
            this.Price = price;
        }

    }
}
