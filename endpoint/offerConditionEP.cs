using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Offer condition endpoints: manage contingencies (finance, inspection, etc.) on offers.
    /// Access: Only offer participants (buyer or property seller) can CRUD conditions.
    /// Conditions track satisfaction status and deadline via is_satisfied/satisfied_at.
    /// </summary>
    public static class offerConditionEP
    {
        /// <summary>
        /// Checks if user is the buyer on the offer or the seller of the related property.
        /// Used by all condition endpoints to enforce participant-only access.
        /// </summary>
        private static async Task<bool> IsOfferParticipant(int offerId, int userId, dbcontext db)
        {
            var offer = await db.offer.FindAsync(offerId);
            if (offer == null) return false;
            if (offer.buyer_id == userId) return true;
            var prop = await db.property.FindAsync(offer.property_id);
            return prop?.sellerid == userId;
        }

        public static void MapOfferConditionEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/offercondition").WithTags(nameof(OfferCondition));

            // Only offer participants can view conditions
            group.MapGet("/{offer_id}", async (int offer_id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();
                if (currentUser.admin != true && !await IsOfferParticipant(offer_id, currentUser.id, db))
                    return Results.Forbid();

                return Results.Ok(await db.offercondition.Where(i => i.offer_id == offer_id).ToListAsync());
            })
            .WithName("GetOfferConditions")
            .WithOpenApi();

            // Only offer participants can add conditions
            group.MapPost("/", async (OfferCondition condition, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(condition.condition_type))
                {
                    return Results.BadRequest(new { error = "condition_type is required" });
                }
                if (condition.offer_id <= 0)
                {
                    return Results.BadRequest(new { error = "Valid offer_id is required" });
                }

                if (currentUser.admin != true && !await IsOfferParticipant(condition.offer_id, currentUser.id, db))
                    return Results.Forbid();

                condition.created_at = DateTime.UtcNow;
                condition.is_satisfied = false;
                db.Add(condition);
                await db.SaveChangesAsync();
                return Results.Created($"/api/offercondition/{condition.id}", condition);
            })
            .WithName("PostOfferCondition")
            .WithOpenApi();

            // Only offer participants can update conditions
            group.MapPut("/", async (OfferCondition condition, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();
                if (currentUser.admin != true && !await IsOfferParticipant(condition.offer_id, currentUser.id, db))
                    return Results.Forbid();

                db.offercondition.Update(condition);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutOfferCondition")
            .WithOpenApi();

            // Only offer participants can satisfy conditions
            group.MapPut("/{id}/satisfy", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var condition = await db.offercondition.Where(i => i.id == id).FirstOrDefaultAsync();
                if (condition == null)
                {
                    return Results.NotFound();
                }

                if (currentUser.admin != true && !await IsOfferParticipant(condition.offer_id, currentUser.id, db))
                    return Results.Forbid();

                condition.is_satisfied = true;
                condition.satisfied_at = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok(condition);
            })
            .WithName("SatisfyOfferCondition")
            .WithOpenApi();

            // Only offer participants can delete conditions
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var condition = await db.offercondition.Where(i => i.id == id).FirstOrDefaultAsync();
                if (condition == null)
                {
                    return Results.NotFound();
                }

                if (currentUser.admin != true && !await IsOfferParticipant(condition.offer_id, currentUser.id, db))
                    return Results.Forbid();

                db.offercondition.Remove(condition);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteOfferCondition")
            .WithOpenApi();
        }
    }
}
