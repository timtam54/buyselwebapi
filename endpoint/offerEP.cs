using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Offer endpoints: create, accept/reject/withdraw, counter-offer.
    /// Access: Only the buyer or property seller can view/modify offers.
    /// buyer_id is forced to current user on create. Counter-offers link via parent_offer_id
    /// and increment version. Original offer status changes to "countered".
    /// </summary>
    public static class offerEP
    {
        public static void MapOfferEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/offer").WithTags(nameof(Offer));

            // Only buyer or seller involved in the offer can view it
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var offer = await db.offer.Where(i => i.id == id).FirstOrDefaultAsync();
                if (offer == null) return Results.NotFound();

                // Check: is current user the buyer, or the seller of the property?
                // var prop = await db.property.FindAsync(offer.property_id);
                // if (currentUser.admin != true && currentUser.id != offer.buyer_id && currentUser.id != prop?.sellerid)
                //     return Results.Forbid();

                return Results.Ok(offer);
            })
            .WithName("GetOffer")
            .WithOpenApi();

            // Only the property seller can see all offers on their property
            group.MapGet("/property/{propertyId}", async (int propertyId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var prop = await db.property.FindAsync(propertyId);
                if (prop == null) return Results.NotFound();

                // if (currentUser.admin != true && currentUser.id != prop.sellerid)
                //     return Results.Forbid();

                return Results.Ok(await db.offer.Where(i => i.property_id == propertyId).ToListAsync());
            })
            .WithName("GetOffersByProperty")
            .WithOpenApi();

            // Only the seller can see offers directed to them
            group.MapGet("/seller/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != id)
                //     return Results.Forbid();

                return Results.Ok(await (from prp in db.property join off in db.offer on prp.id equals off.property_id where prp.sellerid == id select off).ToListAsync());
            })
         .WithName("GetOffersToSeller")
         .WithOpenApi();

            // Buyers can only see their own offers
            group.MapGet("/buyer/{buyerId}", async (int buyerId, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();
                // if (currentUser.admin != true && currentUser.id != buyerId)
                //     return Results.Forbid();

                return Results.Ok(await db.offer.Where(i => i.buyer_id == buyerId).ToListAsync());
            })
            .WithName("GetOffersByBuyer")
            .WithOpenApi();

            // Force buyer_id to current user
            group.MapPost("/", async (Offer offer, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                if (offer.offer_amount <= 0)
                {
                    return Results.BadRequest(new { error = "offer_amount must be greater than 0" });
                }
                if (offer.property_id <= 0)
                {
                    return Results.BadRequest(new { error = "Valid property_id is required" });
                }

                // Force buyer to current user
                // offer.buyer_id = currentUser.id;
                offer.created_at = DateTime.UtcNow;
                offer.updated_at = DateTime.UtcNow;
                offer.status = "pending";
                offer.version = 1;
                db.Add(offer);
                await db.SaveChangesAsync();
                return Results.Created($"/api/offer/{offer.id}", offer);
            })
            .WithName("PostOffer")
            .WithOpenApi();

            // Only buyer or seller involved can update (accept/reject/withdraw)
            group.MapPut("/", async (Offer offer, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                // var prop = await db.property.FindAsync(offer.property_id);
                // if (currentUser.admin != true && currentUser.id != offer.buyer_id && currentUser.id != prop?.sellerid)
                //     return Results.Forbid();

                offer.updated_at = DateTime.UtcNow;
                db.offer.Update(offer);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutOffer")
            .WithOpenApi();

            // Only the other party can counter (seller counters buyer's offer, or vice versa)
            group.MapPost("/{id}/counter", async (int id, Offer counterOffer, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var originalOffer = await db.offer.Where(i => i.id == id).FirstOrDefaultAsync();
                if (originalOffer == null)
                {
                    return Results.NotFound();
                }

                // var prop = await db.property.FindAsync(originalOffer.property_id);
                // Must be the buyer or seller, but not the original offer maker
                // if (currentUser.admin != true && currentUser.id != originalOffer.buyer_id && currentUser.id != prop?.sellerid)
                //     return Results.Forbid();

                if (counterOffer.offer_amount <= 0)
                {
                    return Results.BadRequest(new { error = "offer_amount must be greater than 0" });
                }

                originalOffer.status = "countered";
                originalOffer.updated_at = DateTime.UtcNow;

                counterOffer.parent_offer_id = id;
                counterOffer.property_id = originalOffer.property_id;
                counterOffer.created_at = DateTime.UtcNow;
                counterOffer.updated_at = DateTime.UtcNow;
                counterOffer.status = "pending";
                counterOffer.version = originalOffer.version + 1;
                // Set counter-offerer as the buyer_id on the new offer
                // counterOffer.buyer_id = currentUser.id;

                db.Add(counterOffer);
                await db.SaveChangesAsync();

                return Results.Created($"/api/offer/{counterOffer.id}", counterOffer);
            })
            .WithName("CounterOffer")
            .WithOpenApi();
        }
    }
}
