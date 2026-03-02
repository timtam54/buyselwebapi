namespace buyselwebapi.model
{
    public class PushSubscription
    {
        public int id { get; set; }
        public string email { get; set; }
        public string? endpoint { get; set; }
        public string? p256dh { get; set; }
        public string? auth { get; set; }
        public DateTime createdat { get; set; }
        public DateTime lastUsedat { get; set; }

        public string? devicetoken { get; set; }  // For iOS/Android native
        public string? platform { get; set; }      // "web", "ios", or "android"
        public string? subscriptiontype { get; set; } // "web" or "native"

    }
}
