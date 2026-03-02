namespace buyselwebapi.model
{
    public class AudSummary
    {
        public int id { get; set; }
        public string page { get; set; }
        public int cnt { get; set; }
        public DateTime? wedte { get; set; }


    }
    public class Audit
    {
        public string? ipaddress { get; set; }
        public int id { get; set; }
        public string action { get; set; }
        public string page { get; set; }
        public string username { get; set; }
        public DateTime? dte { get; set; }
        public int? propertyid { get; set; }
    }
}
