using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;

namespace buyselwebapi.endpoint
{
    public static class messageEP
    {
        public static void MapMessageEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/message").WithTags(nameof(messageEP));

            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var msg = await db.message.FindAsync(id);
                return msg;// is not null ? Results.Ok(msg) : Results.NotFound();
            })
            .WithName("GetMessage")
            .WithOpenApi();

            group.MapGet("/conversation/{conversationId}", async (int conversationId, dbcontext db) =>
            {
                var msgs = await db.message
                    .Where(m => m.conversation_id == conversationId)
                    .OrderBy(m => m.created_at)
                    .ToListAsync();
                return msgs;
            })
            .WithName("GetMessagesByConversation")
            .WithOpenApi();

            group.MapGet("/unread/{userId}", async (int userId, dbcontext db) =>
            {
                var msgs = await db.message
                    .Where(m => m.sender_id != userId && m.read_at == null)
                    .ToListAsync();
                return msgs;
            })
            .WithName("GetUnreadMessages")
            .WithOpenApi();

            group.MapGet("/unread/{userId}/{conversationid}", async (int userId,int conversationid, dbcontext db) =>
            {
                var msgs = await db.message
                    .Where(m => m.sender_id != userId && m.read_at == null && m.conversation_id== conversationid)
                    .ToListAsync();
                return msgs;
            })
          .WithName("GetUnreadMessagesInConversation")
          .WithOpenApi();

            group.MapPut("/", async (Message msg, dbcontext db) =>
            {
                db.message.Update(msg);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutMessage")
            .WithOpenApi();

            group.MapPut("/markread/{id}", async (int id, dbcontext db) =>
            {
                var msg = await db.message.FindAsync(id);
                if (msg == null)
                {
                    return Results.NotFound();
                }
                msg.read_at = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("MarkMessageAsRead")
            .WithOpenApi();

            group.MapPut("/markread/{id}/{conversationid}", async (int id,int conversationid, dbcontext db) =>
            {
                var msgs = await db.message.Where(i=>i.conversation_id== conversationid && i.sender_id!=id && i.read_at==null).ToListAsync();
                if (msgs.Count == 0)
                {
                    return Results.Ok();
                }
                foreach (var msg in msgs)
                {
                    msg.read_at = DateTime.UtcNow;
                }
               
                await db.SaveChangesAsync();
                return  Results.Ok();
            })
           .WithName("MarkConvMessagesAsRead")
           .WithOpenApi();

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var msg = await db.message.FindAsync(id);
                if (msg == null)
                {
                    return Results.NotFound();
                }
                db.message.Remove(msg);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteMessage")
            .WithOpenApi();

            group.MapPost("/", async (Message msg, dbcontext db) =>
            {
                db.Add(msg);
                msg.created_at = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Created($"/api/message/{msg.id}", msg);
            })
            .WithName("PostMessage")
            .WithOpenApi();
        }
    }
}