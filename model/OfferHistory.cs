namespace buyselwebapi.model
{
    public class OfferHistory
    {
        public int id { get; set; }
        public int offer_id { get; set; }
        public int actor_id { get; set; }
        public string action { get; set; } = string.Empty;
        public decimal offer_amount { get; set; }
        public string? conditions_json { get; set; }
        public string? message { get; set; }
        public DateTime created_at { get; set; }
    }
}
