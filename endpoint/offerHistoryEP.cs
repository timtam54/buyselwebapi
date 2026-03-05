using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Offer history endpoints: audit trail for offer changes (created, countered, accepted, etc.).
    /// Access: Only offer participants can view/add history. Delete is admin-only.
    /// actor_id is forced to current user on create.
    /// </summary>
    public static class offerHistoryEP
    {
        /// <summary>
        /// Checks if user is the buyer on the offer or the seller of the related property.
        /// </summary>
        private static async Task<bool> IsOfferParticipant(int offerId, int userId, dbcontext db)
        {
            var offer = await db.offer.FindAsync(offerId);
            if (offer == null) return false;
            if (offer.buyer_id == userId) return true;
            var prop = await db.property.FindAsync(offer.property_id);
            return prop?.sellerid == userId;
        }

        public static void MapOfferHistoryEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/offerhistory").WithTags(nameof(OfferHistory));

            // Only offer participants can view history
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var history = await db.offerhistory.Where(i => i.id == id).FirstOrDefaultAsync();
                if (history == null) return Results.NotFound();

                if (currentUser.admin != true && !await IsOfferParticipant(history.offer_id, currentUser.id, db))
                    return Results.Forbid();

                return Results.Ok(history);
            })
            .WithName("GetOfferHistory")
            .WithOpenApi();

            group.MapGet("/offer/{offerId}", async (int offerId, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (currentUser.admin != true && !await IsOfferParticipant(offerId, currentUser.id, db))
                    return Results.Forbid();

                return Results.Ok(await db.offerhistory
                    .Where(i => i.offer_id == offerId)
                    .OrderByDescending(i => i.created_at)
                    .ToListAsync());
            })
            .WithName("GetOfferHistoryByOffer")
            .WithOpenApi();

            // Only offer participants can add history
            group.MapPost("/", async (OfferHistory history, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (history.offer_id <= 0)
                {
                    return Results.BadRequest(new { error = "Valid offer_id is required" });
                }
                if (string.IsNullOrWhiteSpace(history.action))
                {
                    return Results.BadRequest(new { error = "action is required" });
                }

                if (currentUser.admin != true && !await IsOfferParticipant(history.offer_id, currentUser.id, db))
                    return Results.Forbid();

                // Force actor to current user
                history.actor_id = currentUser.id;
                history.created_at = DateTime.UtcNow;
                db.Add(history);
                await db.SaveChangesAsync();
                return Results.Created($"/api/offerhistory/{history.id}", history);
            })
            .WithName("PostOfferHistory")
            .WithOpenApi();

            // Only offer participants can update history
            group.MapPut("/", async (OfferHistory history, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (currentUser.admin != true && !await IsOfferParticipant(history.offer_id, currentUser.id, db))
                    return Results.Forbid();

                db.offerhistory.Update(history);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutOfferHistory")
            .WithOpenApi();

            // Admin only - delete history records
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                if (!await AuthHelper.IsAdmin(principal, db))
                    return Results.Forbid();

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
