namespace buyselwebapi.model
{
    public class PropertyBuyerDoc
    {
        public int id { get; set; }
        public int propertyid { get; set; }
        public DateTime? dte { get; set; }
        public int buyerid { get; set; }
        public string requestdoc { get; set; }
        public string? action { get; set; }
       // public string? status { get; set; }
    }
}
