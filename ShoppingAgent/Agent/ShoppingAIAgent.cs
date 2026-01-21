using Google.GenAI;
using Google.GenAI.Types;
using ShoppingAgent.Data;
using ShoppingAgent.Services;

namespace ShoppingAgent.Agent
{
    public class ShoppingAIAgent:IAgent
    {
        private string APIkey;
        private Client client;
        private int? lowerlimit;
        private int? upperlimit;
        private string category;

        // Set up configuration to connect to Gemini API
        public ShoppingAIAgent()
        {
            var config = new ConfigurationBuilder()
                            .AddUserSecrets<ShoppingAIAgent>()
                            .Build();
            this.APIkey = config["GOOGLE_API_KEY"];
            this.client = new Client(apiKey: this.APIkey);
        }

        public string CleanJustification(string justification)
        {
            if (string.IsNullOrEmpty(justification))
            {
                return "No justification provided.";
            }
            
            var cleaned = justification.Replace('*', ' ').Trim();
            return cleaned;
        }

        /// <summary>
        /// Call the AI to Analyze the user's purchase history Based on his purchase history:
        /// 1. Determine their preferred price range(lower and upper limits)
        /// 2. Recommend a product's category from: {categories}
        /// </summary>
        /// <param name="buyersHistory"> User's Purchase History</param>
        /// <param name="categories"> List of available categories</param>
        /// <returns></returns>
        public async Task<GenerateContentResponse> RankProductsAIRequest(string buyersHistory, string categories)
        {
            var message = $@"Analyze the user's purchase history: {buyersHistory} and based on it:
1. Determine their preferred price range (lower and upper limits)
2. Recommend a product's category from: {categories}
3. You MUST call the rank_products function with the recommended category and price range.
Do not provide any text response - only call the function.";

            var rankProducts = new Google.GenAI.Types.FunctionDeclaration
            {
                Name = "rank_products",
                Description = "Gets the top 3 matching products from a category based on price range. This function must be called to retrieve actual product data.",
                Parameters = new Google.GenAI.Types.Schema
                {
                    Type = Google.GenAI.Types.Type.OBJECT,
                    Properties = new Dictionary<string, Schema>
                    {
                        ["category"] = new Schema { Type = Google.GenAI.Types.Type.STRING, Description = "The product category to search in" },
                        ["lowerlimit"] = new Schema { Type = Google.GenAI.Types.Type.NUMBER, Description = "The minimum price in the user's preferred range" },
                        ["upperlimit"] = new Schema { Type = Google.GenAI.Types.Type.NUMBER, Description = "The maximum price in the user's preferred range" }
                    },
                    Required = new List<string> { "category", "lowerlimit", "upperlimit" }
                }
            };

            var tools = new List<Google.GenAI.Types.Tool>
            {
                new Google.GenAI.Types.Tool
                {
                   FunctionDeclarations = new List<Google.GenAI.Types.FunctionDeclaration> { rankProducts }
                }
            };

            return await client.Models.GenerateContentAsync(
                              model: "models/gemini-2.5-flash-lite",
                              contents: message,
                              config: new GenerateContentConfig
                              {
                                  Tools = tools,
                                  MaxOutputTokens = 200
                              });
        }


        /// <summary>
        /// Sends a follow-up request to generate a justification for the recommended product category, price range, and
        /// specific product recommendations based on the user's purchase history.
        /// </summary>
        /// <param name="functionResultJson">A string containing the results from the rank_products function, representing the recommended
        /// products.</param>
        public async Task<string> followupRequest(string buyersHistory)
        {
            string followUpMessage = $@"Based on the user's purchase history: {buyersHistory} Now explain and justify:
1. Why you recommended the '{category}' category based on the user's purchase history
2. Why this price range (${lowerlimit} - ${upperlimit}) matches their preferences";

            var followUpResponse = await client.Models.GenerateContentAsync(
                model: "models/gemini-2.5-flash-lite",
                contents: followUpMessage,
                config: new GenerateContentConfig { MaxOutputTokens = 200 }
            );

            var justification = followUpResponse.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            
            return CleanJustification(justification);

        }

        public async Task<AgentOutput> LLMbasedRecommendation(string buyersHistory, string categories)
        {

            // Step 1: Get the AI 's recommended category and price range via function call
            var response = await RankProductsAIRequest(buyersHistory, categories);

            var AIfunctionCall = response.Candidates?
                        .FirstOrDefault()?
                        .Content?
                        .Parts?
                        .FirstOrDefault()?
                        .FunctionCall;
            AgentOutput agentOutput;
            
            if (AIfunctionCall != null)
            {
                //Console.WriteLine("=== Function Called ===");
                //Console.WriteLine($"Function: {AIfunctionCall.Name}");
                //Console.WriteLine($"Arguments: {JsonSerializer.Serialize(AIfunctionCall.Args, new JsonSerializerOptions { WriteIndented = true })}\n");

                // Extract arguments
                var args = AIfunctionCall.Args;
                category = args != null && args.TryGetValue("category", out var catObj) ? catObj?.ToString() ?? "" : "";
                lowerlimit = args != null && args.TryGetValue("lowerlimit", out var lowObj) && int.TryParse(lowObj?.ToString(), out var lowVal) ? lowVal : (int?)null;
                upperlimit = args != null && args.TryGetValue("upperlimit", out var upObj) && int.TryParse(upObj?.ToString(), out var upVal) ? upVal : (int?)null;

                // Execute the function
                List<Item> rankedProducts = RankProducts.RankProductsCategory(category, lowerlimit, upperlimit);

                // Step 2: Send function results back and get justification
                //string functionResultJson = JsonSerializer.Serialize(rankedProducts.Select(p => new { p.Product, p.Category, p.Price }));
                string justification = await followupRequest(buyersHistory);
                agentOutput = new AgentOutput() { RecommendedProducts = rankedProducts, Justification = justification, Category = category };
                Console.WriteLine($"\n AI Recommendation Justification");
                Console.WriteLine(justification ?? "No justification provided");
            }

            else
            {
                var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                agentOutput = new AgentOutput() { RecommendedProducts = null, Justification = text,Category = category };

                Console.WriteLine("No function call detected!");
                Console.WriteLine("The AI returned text instead:");
                Console.WriteLine(text ?? "No response");
            }
            return agentOutput;
        }

    }
}
