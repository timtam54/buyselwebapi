
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class offerEP
    {

        public static void MapOfferEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/offer").WithTags(nameof(Offer));

            // GET /api/offer/{id} -- View single offer
            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var offer = await db.offer.Where(i => i.id == id).FirstOrDefaultAsync();
                return offer;
            })
            .WithName("GetOffer")
            .WithOpenApi();

            // GET /api/offer/property/{propertyId} -- Seller sees all offers on their property
            group.MapGet("/property/{propertyId}", async (int propertyId, dbcontext db) =>
            {
                return await db.offer.Where(i => i.property_id == propertyId).ToListAsync();
            })
            .WithName("GetOffersByProperty")
            .WithOpenApi();
            group.MapGet("/seller/{id}", async (int id, dbcontext db) =>
            {
                return await (from prp in db.property join off in db.offer on prp.id equals off.property_id where prp.sellerid == id select off).ToListAsync();
            })
         .WithName("GetOffersToSeller")
         .WithOpenApi();

            // GET /api/offer/buyer/{buyerId} -- Buyer sees all their offers
            group.MapGet("/buyer/{buyerId}", async (int buyerId, dbcontext db) =>
            {
                return await db.offer.Where(i => i.buyer_id == buyerId).ToListAsync();
            })
            .WithName("GetOffersByBuyer")
            .WithOpenApi();

            // POST /api/offer -- Create offer
            group.MapPost("/", async (Offer offer, dbcontext db) =>
            {
                offer.created_at = DateTime.UtcNow.AddHours(10);
                offer.updated_at = DateTime.UtcNow.AddHours(10);
                offer.status = "pending";
                offer.version = 1;
                db.Add(offer);
                await db.SaveChangesAsync();
                return Results.Created($"/api/offer/{offer.id}", offer);
            })
            .WithName("PostOffer")
            .WithOpenApi();

            // PUT /api/offer -- Accept/reject/withdraw
            group.MapPut("/", async (Offer offer, dbcontext db) =>
            {
                offer.updated_at = DateTime.UtcNow.AddHours(10);
                db.offer.Update(offer);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutOffer")
            .WithOpenApi();

            // POST /api/offer/{id}/counter -- Counter-offer
            group.MapPost("/{id}/counter", async (int id, Offer counterOffer, dbcontext db) =>
            {
                var originalOffer = await db.offer.Where(i => i.id == id).FirstOrDefaultAsync();
                if (originalOffer == null)
                {
                    return Results.NotFound();
                }

                // Update original offer status to countered
                originalOffer.status = "countered";
                originalOffer.updated_at = DateTime.UtcNow.AddHours(10);

                // Create new counter offer
                counterOffer.parent_offer_id = id;
                counterOffer.property_id = originalOffer.property_id;
                counterOffer.created_at = DateTime.UtcNow.AddHours(10);
                counterOffer.updated_at = DateTime.UtcNow.AddHours(10);
                counterOffer.status = "pending";
                counterOffer.version = originalOffer.version + 1;

                db.Add(counterOffer);
                await db.SaveChangesAsync();

                return Results.Created($"/api/offer/{counterOffer.id}", counterOffer);
            })
            .WithName("CounterOffer")
            .WithOpenApi();
        }

    }
    
}