
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
namespace buyselwebapi.data
{
    public class dbcontext : DbContext
    {
        public dbcontext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<OfferHistory> offerhistory { get; set; }
        public DbSet<OfferCondition> offercondition { get; set; }
        public DbSet<Offer> offer { get; set; }
        public DbSet<UserPropertyFav> userpropertyfav { get; set; }
        public DbSet<Conversation> conversation { get; set; }
        public DbSet<Message> message { get; set; }
        public DbSet<PushSubscription> pushsubscriptions { get; set; }
        public DbSet<Property> property { get; set; }
        public DbSet<User> user { get; set; }
        public DbSet<PropertyBuyerDoc> propertybuyerdoc { get; set; }
        public DbSet<Badge> badge { get; set; }
        public DbSet<PropertyPhoto> propertyphoto { get; set; }
        public DbSet<AudSummary> audsummary { get; set; }
        public DbSet<Audit> audit { get; set; }
        public DbSet<ConversationCount> conversationcount { get; set; }
    }
}
