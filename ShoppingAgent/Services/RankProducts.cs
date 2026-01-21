using ShoppingAgent.Data;
using System.Text.Json;

namespace ShoppingAgent.Services
{
    public static class RankProducts
    {
        private static IEnumerable<Item> GetAllProductsByCategory(string category)
        {
            string RootPath = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;
            string productcategories = Path.Combine(RootPath, "Models/productcategory.json");
            string productCategoryJson = File.ReadAllText(productcategories);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<Item> products = JsonSerializer.Deserialize<List<Item>>(productCategoryJson, jsonOptions);
            return products.Where(i => i.Category == category);
        }

        private static List<Item> AddProductsNeartoRange(List<Item> rankedProducts, List<Item> lowerProds, List<Item> upperProds, int? lowerlimit, int? upperlimit, int countNeededProducts)
        {
            int i = 0, j = 0;
            while (countNeededProducts > 0 && (i < lowerProds.Count) && (j < upperProds.Count))
            {
                if ((lowerlimit - lowerProds[i].Price) <= (upperProds[j].Price - upperlimit))
                {
                    rankedProducts.Add(lowerProds[i]);
                    i++;
                }
                else
                {
                    rankedProducts.Add(upperProds[j]);
                    j++;
                }
                countNeededProducts--;
            }

            while (countNeededProducts > 0 && i < lowerProds.Count)
            {
                rankedProducts.Add(lowerProds[i]);
                i++;
                countNeededProducts--;
            }
            while (countNeededProducts > 0 && j < upperProds.Count)
            {
                rankedProducts.Add(upperProds[j]);
                j++;
                countNeededProducts--;
            }
            return rankedProducts;
        }
        public static List<Item> RankProductsCategory(string category, int? lowerlimit, int? upperlimit)
        {
            IEnumerable<Item> products = GetAllProductsByCategory(category);

            List<Item> sortedProducts = products.OrderBy(i => i.Price).ToList();
            List<Item> rankedProducts = new List<Item>();
            List<Item> lowerProds = new List<Item>();
            List<Item> upperProds = new List<Item>();

            // First, try to get products within the specified range
            if (lowerlimit.HasValue && upperlimit.HasValue)
            {
                rankedProducts = sortedProducts.Where(i => (i.Price >= lowerlimit.Value) && (i.Price <= upperlimit.Value)).ToList();
                if (rankedProducts.Count() >= 3)
                {
                    return rankedProducts.Take(3).ToList();
                }
            }
            // If not enough products in range, get products just outside the range
            // on both sides
            if (lowerlimit.HasValue)
            {
                lowerProds = sortedProducts.Where(i => i.Price < lowerlimit.Value)
                                               .OrderByDescending(i => i.Price)
                                               .Take(3 - rankedProducts.Count())
                                               .ToList();
            }

            if (upperlimit.HasValue)
            {
                upperProds = sortedProducts
                                    .Where(i => i.Price > upperlimit.Value)
                                    .Take(3 - rankedProducts.Count())
                                    .ToList();
            }
            // Add nearest products to the range until we have 3 products
            rankedProducts = AddProductsNeartoRange(rankedProducts, lowerProds, upperProds, lowerlimit, upperlimit, 3 - rankedProducts.Count());
            return rankedProducts.Take(3)
                                 .ToList();
        }
    }
}
