using buyselwebapi.data;
using buyselwebapi.model;
using IncidentWebAPI.endpoint;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace buyselwebapi.endpoint
{
    public static class conversationEP
    {
        public static void MapConversationEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/conversation").WithTags(nameof(conversationEP));


            group.MapGet("/unread/{userId}", async (int userId, dbcontext db) =>
            {
                var sched = await db.conversationcount.FromSqlRaw($"exec unreadConv {userId}").ToListAsync();
                return sched;
            })
           .WithName("GetUnreadConv")
           .WithOpenApi();

            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var conv = await db.conversation.FindAsync(id);
                return conv;//is not null ? Results.Ok(conv) : Results.NotFound();
            })
            .WithName("GetConversation")
            .WithOpenApi();
            group.MapGet("/user/{id}", async (int id, dbcontext db) =>
            {
                var conv = await db.conversation.Where(i=>i.buyer_id==id || i.seller_id==id).ToListAsync();
                return conv;//is not null ? Results.Ok(conv) : Results.NotFound();
            })
     .WithName("GetConversationUser")
     .WithOpenApi();
            group.MapGet("/property/{propertyId}", async (int propertyId, dbcontext db) =>
            {
                var convs = await db.conversation.Where(c => c.property_id == propertyId).ToListAsync();
                return convs;
            })
            .WithName("GetConversationsByProperty")
            .WithOpenApi();

            group.MapGet("/buyer/{buyerId}", async (int buyerId, dbcontext db) =>
            {
                var convs = await db.conversation.Where(c => c.buyer_id == buyerId).ToListAsync();
                return convs;
            })
            .WithName("GetConversationsByBuyer")
            .WithOpenApi();

            group.MapGet("/seller/{sellerId}", async (int sellerId, dbcontext db) =>
            {
                var convs = await db.conversation.Where(c => c.seller_id == sellerId).ToListAsync();
                return convs;
            })
            .WithName("GetConversationsBySeller")
            .WithOpenApi();

            group.MapPut("/", async (Conversation conv, dbcontext db) =>
            {
                db.conversation.Update(conv);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutConversation")
            .WithOpenApi();

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var conv = await db.conversation.FindAsync(id);
                if (conv == null)
                {
                    return Results.NotFound();
                }
                db.conversation.Remove(conv);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteConversation")
            .WithOpenApi();

            group.MapPost("/", async (Conversation conv, dbcontext db) =>
            {
                db.Add(conv);
                conv.created_at = DateTime.UtcNow;
                await db.SaveChangesAsync();
                try
                {
                    var buyer = await db.user.Where(i => i.id == conv.buyer_id).FirstOrDefaultAsync();
                    var seller = await db.user.Where(i => i.id == conv.seller_id).FirstOrDefaultAsync();
                    await auditEP.Audit(buyer.email, db, "Chat", "Initiated Conversation with Seller " + seller.email,0);
                }
                catch (Exception ex)
                {
                }
                return Results.Created($"/api/conversation/{conv.id}", conv);
            })
            .WithName("PostConversation")
            .WithOpenApi();
        }
    }
}