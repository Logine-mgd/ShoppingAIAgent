using Google.GenAI.Types;
using ShoppingAgent.Data;

namespace ShoppingAgent.Agent
{
    public interface IAgent
    {
        public string CleanJustification(string justification); 
        public Task<AgentOutput> LLMbasedRecommendation(string message, string categories);
        public Task<string> followupRequest(string buyersHistory);
        public Task<GenerateContentResponse> RankProductsAIRequest(string buyersHistory, string categories);

    }
}
