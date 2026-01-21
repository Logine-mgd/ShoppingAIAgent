namespace ShoppingAgent.Data
{
    public class AgentOutput
    {
        public List<Item> RecommendedProducts { get; set; }
        public string Justification { get; set; }
        public string Category { get; set; }
    }
}
