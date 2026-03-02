namespace buyselwebapi.model
{
    public class PropertyPhoto
    {
        public int id { get; set; }

        public int propertyid { get; set; }

        public string photobloburl { get; set; }

        public string? title { get; set; }
        public DateTime? dte { get; set; }
        public bool? doc { get; set; }
    }
}
