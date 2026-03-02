using System;

namespace buyselwebapi.model
{
    public class Offer
    {
        public int id { get; set; }
        public int property_id { get; set; }
       // public virtual Property property { get; set; }
        public int buyer_id { get; set; }
       // public virtual User buyer { get; set; }
        public string status { get; set; } = "pending";
        public decimal offer_amount { get; set; }
        public decimal? deposit_amount { get; set; }
        public int? settlement_days { get; set; }
        public int? finance_days { get; set; }
        public int? inspection_days { get; set; }
        public string? conditions_json { get; set; }
        public DateTime? expires_at { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
        public DateTime updated_at { get; set; } = DateTime.Now;
        public int? parent_offer_id { get; set; }
       // public virtual Offer? parent_offer { get; set; }
        public int version { get; set; } = 1;
    }
}
