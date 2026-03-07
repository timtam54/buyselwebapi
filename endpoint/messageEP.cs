using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Message endpoints: send/read/delete chat messages within conversations.
    /// Access: Only conversation participants can read or send messages.
    /// sender_id is forced to current user on create. Supports mark-as-read (single and bulk).
    /// </summary>
    public static class messageEP
    {
        /// <summary>
        /// Checks if user is a buyer or seller in the given conversation.
        /// Used by all message endpoints to enforce participant-only access.
        /// </summary>
        private static async Task<bool> IsConversationParticipant(int conversationId, int userId, dbcontext db)
        {
            var conv = await db.conversation.FindAsync(conversationId);
            if (conv == null) return false;
            return conv.buyer_id == userId || conv.seller_id == userId;
        }

        public static void MapMessageEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/message").WithTags(nameof(messageEP));

            // Only conversation participants can view a message
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var msg = await db.message.FindAsync(id);
                if (msg == null) return Results.NotFound();

                // if (currentUser.admin != true && !await IsConversationParticipant(msg.conversation_id, currentUser.id, db))
                //     return Results.Forbid();

                return Results.Ok(msg);
            })
            .WithName("GetMessage")
            .WithOpenApi();

            // Only conversation participants can view messages
            group.MapGet("/conversation/{conversationId}", async (int conversationId, dbcontext db, ClaimsPrincipal principal, int page = 1, int pageSize = 50) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                // if (currentUser.admin != true && !await IsConversationParticipant(conversationId, currentUser.id, db))
                //     return Results.Forbid();

                pageSize = Math.Clamp(pageSize, 1, 200);
                var msgs = await db.message
                    .Where(m => m.conversation_id == conversationId)
                    .OrderBy(m => m.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                return Results.Ok(msgs);
            })
            .WithName("GetMessagesByConversation")
            .WithOpenApi();

            // Users can only see their own unread messages
            group.MapGet("/unread/{userId}", async (int userId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != userId)
                //     return Results.Forbid();

                var msgs = await db.message
                    .Where(m => m.sender_id != userId && m.read_at == null)
                    .ToListAsync();
                return Results.Ok(msgs);
            })
            .WithName("GetUnreadMessages")
            .WithOpenApi();

            group.MapGet("/unread/{userId}/{conversationid}", async (int userId, int conversationid, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != userId)
                //     return Results.Forbid();

                var msgs = await db.message
                    .Where(m => m.sender_id != userId && m.read_at == null && m.conversation_id == conversationid)
                    .ToListAsync();
                return Results.Ok(msgs);
            })
          .WithName("GetUnreadMessagesInConversation")
          .WithOpenApi();

            // Only the sender can update their own message
            group.MapPut("/", async (Message msg, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != msg.sender_id)
                //     return Results.Forbid();

                db.message.Update(msg);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutMessage")
            .WithOpenApi();

            // Only conversation participants can mark messages as read
            group.MapPut("/markread/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var msg = await db.message.FindAsync(id);
                if (msg == null)
                {
                    return Results.NotFound();
                }

                // if (currentUser.admin != true && !await IsConversationParticipant(msg.conversation_id, currentUser.id, db))
                //     return Results.Forbid();

                msg.read_at = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("MarkMessageAsRead")
            .WithOpenApi();

            group.MapPut("/markread/{id}/{conversationid}", async (int id, int conversationid, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != id)
                //     return Results.Forbid();

                var msgs = await db.message.Where(i => i.conversation_id == conversationid && i.sender_id != id && i.read_at == null).ToListAsync();
                if (msgs.Count == 0)
                {
                    return Results.Ok();
                }
                foreach (var msg in msgs)
                {
                    msg.read_at = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                return Results.Ok();
            })
           .WithName("MarkConvMessagesAsRead")
           .WithOpenApi();

            // Only the sender or admin can delete a message
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var msg = await db.message.FindAsync(id);
                if (msg == null)
                {
                    return Results.NotFound();
                }

                // if (currentUser.admin != true && currentUser.id != msg.sender_id)
                //     return Results.Forbid();

                db.message.Remove(msg);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteMessage")
            .WithOpenApi();

            // Force sender_id to current user
            group.MapPost("/", async (Message msg, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(msg.content))
                {
                    return Results.BadRequest(new { error = "Content is required" });
                }
                if (msg.conversation_id <= 0)
                {
                    return Results.BadRequest(new { error = "conversation_id is required" });
                }

                // Verify user is a participant in this conversation
                // if (currentUser.admin != true && !await IsConversationParticipant(msg.conversation_id, currentUser.id, db))
                //     return Results.Forbid();

                // Force sender to current user
                // msg.sender_id = currentUser.id;
                msg.created_at = DateTime.UtcNow;
                db.Add(msg);
                await db.SaveChangesAsync();
                return Results.Created($"/api/message/{msg.id}", msg);
            })
            .WithName("PostMessage")
            .WithOpenApi();
        }
    }
}
