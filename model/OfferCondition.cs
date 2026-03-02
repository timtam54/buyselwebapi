namespace buyselwebapi.model
{
    public class OfferCondition
    {
        public int id { get; set; }
        public int offer_id { get; set; }
        public string condition_type { get; set; } = string.Empty;
        public string? description { get; set; }
        public int? days_to_satisfy { get; set; }
        public bool? is_satisfied { get; set; }
        public DateTime? satisfied_at { get; set; }
        public DateTime created_at { get; set; }
    }
}
