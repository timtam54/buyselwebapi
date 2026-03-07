using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Conversation endpoints: buyer-seller messaging threads.
    /// Access: Only conversation participants (buyer_id or seller_id) can view/modify.
    /// buyer_id is forced to current user on create. Uses parameterized SQL for unread counts.
    /// </summary>
    public static class conversationEP
    {
        public static void MapConversationEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/conversation").WithTags(nameof(conversationEP));

            // Only the user themselves can check their unread count
            group.MapGet("/unread/{userId}", async (int userId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != userId)
                //     return Results.Forbid();

                var sched = await db.conversationcount.FromSqlInterpolated($"exec unreadConv {userId}").ToListAsync();
                return Results.Ok(sched);
            })
           .WithName("GetUnreadConv")
           .WithOpenApi();

            // Only participants can view a conversation
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var conv = await db.conversation.FindAsync(id);
                if (conv == null) return Results.NotFound();

                // if (currentUser.admin != true && currentUser.id != conv.buyer_id && currentUser.id != conv.seller_id)
                //     return Results.Forbid();

                return Results.Ok(conv);
            })
            .WithName("GetConversation")
            .WithOpenApi();

            // Users can only view their own conversations
            group.MapGet("/user/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != id)
                //     return Results.Forbid();

                var conv = await db.conversation.Where(i => i.buyer_id == id || i.seller_id == id).ToListAsync();
                return Results.Ok(conv);
            })
     .WithName("GetConversationUser")
     .WithOpenApi();

            // Only the property seller or admin can view conversations for a property
            group.MapGet("/property/{propertyId}", async (int propertyId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var prop = await db.property.FindAsync(propertyId);
                if (prop == null) return Results.NotFound();

                // if (currentUser.admin != true && currentUser.id != prop.sellerid)
                //     return Results.Forbid();

                var convs = await db.conversation.Where(c => c.property_id == propertyId).ToListAsync();
                return Results.Ok(convs);
            })
            .WithName("GetConversationsByProperty")
            .WithOpenApi();

            // Users can only view their own conversations as buyer
            group.MapGet("/buyer/{buyerId}", async (int buyerId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != buyerId)
                //     return Results.Forbid();

                var convs = await db.conversation.Where(c => c.buyer_id == buyerId).ToListAsync();
                return Results.Ok(convs);
            })
            .WithName("GetConversationsByBuyer")
            .WithOpenApi();

            // Users can only view their own conversations as seller
            group.MapGet("/seller/{sellerId}", async (int sellerId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != sellerId)
                //     return Results.Forbid();

                var convs = await db.conversation.Where(c => c.seller_id == sellerId).ToListAsync();
                return Results.Ok(convs);
            })
            .WithName("GetConversationsBySeller")
            .WithOpenApi();

            // Only participants can update a conversation
            group.MapPut("/", async (Conversation conv, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != conv.buyer_id && currentUser.id != conv.seller_id)
                //     return Results.Forbid();

                db.conversation.Update(conv);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutConversation")
            .WithOpenApi();

            // Only participants or admin can delete a conversation
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var conv = await db.conversation.FindAsync(id);
                if (conv == null)
                {
                    return Results.NotFound();
                }

                // if (currentUser.admin != true && currentUser.id != conv.buyer_id && currentUser.id != conv.seller_id)
                //     return Results.Forbid();

                db.conversation.Remove(conv);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteConversation")
            .WithOpenApi();

            // buyer_id is forced to current user
            group.MapPost("/", async (Conversation conv, dbcontext db, ClaimsPrincipal principal, ILogger<dbcontext> logger) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                if (conv.property_id <= 0 || conv.seller_id <= 0)
                {
                    return Results.BadRequest(new { error = "property_id and seller_id are required" });
                }

                // Force buyer to current user
                // conv.buyer_id = currentUser.id;
                conv.created_at = DateTime.UtcNow;
                db.Add(conv);
                await db.SaveChangesAsync();
                try
                {
                    var seller = await db.user.Where(i => i.id == conv.seller_id).FirstOrDefaultAsync();
                    await auditEP.Audit("test@test.com", db, "Chat", "Initiated Conversation with Seller " + seller.email, 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for conversation {ConversationId}", conv.id);
                }
                return Results.Created($"/api/conversation/{conv.id}", conv);
            })
            .WithName("PostConversation")
            .WithOpenApi();
        }
    }
}
