using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace IncidentWebAPI.endpoint
{
    public static class offerHistoryEP
    {
        public static void MapOfferHistoryEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/offerhistory").WithTags(nameof(OfferHistory));

            // GET /api/offerhistory/{id} -- View single history record
            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var history = await db.offerhistory.Where(i => i.id == id).FirstOrDefaultAsync();
                return history;
            })
            .WithName("GetOfferHistory")
            .WithOpenApi();

            // GET /api/offerhistory/offer/{offerId} -- Get all history for an offer
            group.MapGet("/offer/{offerId}", async (int offerId, dbcontext db) =>
            {
                return await db.offerhistory
                    .Where(i => i.offer_id == offerId)
                    .OrderByDescending(i => i.created_at)
                    .ToListAsync();
            })
            .WithName("GetOfferHistoryByOffer")
            .WithOpenApi();

            // POST /api/offerhistory -- Create history record
            group.MapPost("/", async (OfferHistory history, dbcontext db) =>
            {
                history.created_at = DateTime.UtcNow.AddHours(10);
                db.Add(history);
                await db.SaveChangesAsync();
                return Results.Created($"/api/offerhistory/{history.id}", history);
            })
            .WithName("PostOfferHistory")
            .WithOpenApi();

            // PUT /api/offerhistory -- Update history record
            group.MapPut("/", async (OfferHistory history, dbcontext db) =>
            {
                db.offerhistory.Update(history);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutOfferHistory")
            .WithOpenApi();

            // DELETE /api/offerhistory/{id} -- Delete history record
            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var history = await db.offerhistory.Where(i => i.id == id).FirstOrDefaultAsync();
                if (history == null)
                {
                    return Results.NotFound();
                }
                db.offerhistory.Remove(history);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteOfferHistory")
            .WithOpenApi();
        }
    }
}