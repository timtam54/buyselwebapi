using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;

namespace IncidentWebAPI.endpoint
{
    public static class offerConditionEP
    {
        public static void MapOfferConditionEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/offercondition").WithTags(nameof(OfferCondition));

            // GET /api/offercondition/{offer_id} -- Get all conditions for an offer
            group.MapGet("/{offer_id}", async (int offer_id, dbcontext db) =>
            {
                return await db.offercondition.Where(i => i.offer_id == offer_id).ToListAsync();
            })
            .WithName("GetOfferConditions")
            .WithOpenApi();

            // POST /api/offercondition -- Create condition
            group.MapPost("/", async (OfferCondition condition, dbcontext db) =>
            {
                condition.created_at = DateTime.UtcNow.AddHours(10);
                condition.is_satisfied = false;
                db.Add(condition);
                await db.SaveChangesAsync();
                return Results.Created($"/api/offercondition/{condition.id}", condition);
            })
            .WithName("PostOfferCondition")
            .WithOpenApi();

            // PUT /api/offercondition -- Update condition
            group.MapPut("/", async (OfferCondition condition, dbcontext db) =>
            {
                db.offercondition.Update(condition);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("PutOfferCondition")
            .WithOpenApi();

            // PUT /api/offercondition/{id}/satisfy -- Mark condition as satisfied
            group.MapPut("/{id}/satisfy", async (int id, dbcontext db) =>
            {
                var condition = await db.offercondition.Where(i => i.id == id).FirstOrDefaultAsync();
                if (condition == null)
                {
                    return Results.NotFound();
                }
                condition.is_satisfied = true;
                condition.satisfied_at = DateTime.UtcNow.AddHours(10);
                await db.SaveChangesAsync();
                return Results.Ok(condition);
            })
            .WithName("SatisfyOfferCondition")
            .WithOpenApi();

            // DELETE /api/offercondition/{id} -- Delete condition
            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var condition = await db.offercondition.Where(i => i.id == id).FirstOrDefaultAsync();
                if (condition == null)
                {
                    return Results.NotFound();
                }
                db.offercondition.Remove(condition);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .WithName("DeleteOfferCondition")
            .WithOpenApi();
        }
    }
}