using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShoppingAgent.Agent;
using ShoppingAgent.Data;
using ShoppingAgent.Services;
using System.Text.Json;

namespace ShoppingAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingAgentController : ControllerBase
    {
        string RootPath;
        string buyershistorypath;
        string categoriespath;
        string buyersHistory;
        string categories;
        
        public ShoppingAgentController()
        {
            RootPath = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;
            buyershistorypath = Path.Combine(RootPath, "Models/buyershistory.json");
            categoriespath = Path.Combine(RootPath, "Models/categories.json");

            buyersHistory = System.IO.File.ReadAllText(buyershistorypath);
            categories = System.IO.File.ReadAllText(categoriespath);
        }

        [HttpGet]
        public async Task<IActionResult> GetModelOutput()
        {
            ShoppingAIAgent shoppingAgent = new ShoppingAIAgent();
            var shoppingAgentOutput = await shoppingAgent.LLMbasedRecommendation(buyersHistory, categories);
            return Ok(shoppingAgentOutput);
        }
        
        [HttpPost]
        public IActionResult AddPurchaseData(Item newPurchase)
        {
            var user = JsonSerializer.Deserialize<BuyerHistory>(buyersHistory);

            user.history.Add(new Item
            {
                Product = newPurchase.Product,
                Category = newPurchase.Category,
                Price = newPurchase.Price
            });

            var updatedJson = JsonSerializer.Serialize(user, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(buyershistorypath, updatedJson);
            buyersHistory = updatedJson;

            return Ok(new
            {
                message = "Purchase added successfully",
                item = newPurchase
            });
            
        }
    }
}
