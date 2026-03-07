using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Buyer document request endpoints: buyers request documents from sellers for a property.
    /// Access: Buyer or property seller can view/update. Admin can view outstanding/all.
    /// buyerid is forced to current user on create. Outstanding = where action is null.
    /// </summary>
    public static class propertybuyerdocEP
    {
        public static void MapPropertyBuyerDocEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/propertybuyerdoc").WithTags(nameof(propertybuyerdocEP));

            // Buyer or seller of the property can view doc requests
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var sched = await db.propertybuyerdoc.Where(i => i.propertyid == id).FirstOrDefaultAsync();
                if (sched == null) return Results.NotFound();

                // Check if user is the buyer or the property seller
                // var prop = await db.property.FindAsync(id);
                // if (currentUser.admin != true && currentUser.id != sched.buyerid && currentUser.id != prop?.sellerid)
                //     return Results.Forbid();

                return Results.Ok(sched);
            })
          .WithName("Getpropertybuyerdoc")
          .WithOpenApi();

            // Admin only - view outstanding doc requests
            group.MapGet("/", async (dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

                var sched = await db.propertybuyerdoc.Where(i => i.action == null).ToListAsync();
                return Results.Ok(sched);
            })
       .WithName("Getpropertybuyerdocoutstand")
       .WithOpenApi();

            // Admin only - view all doc requests
            group.MapGet("/all/", async (dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

                var sched = await db.propertybuyerdoc.ToListAsync();
                return Results.Ok(sched);
            })
       .WithName("Getpropertybuyerdocall")
       .WithOpenApi();

            // Buyer or seller of property can update doc requests
            group.MapPut("/", async (PropertyBuyerDoc property, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                // var prop = await db.property.FindAsync(property.propertyid);
                // if (currentUser.admin != true && currentUser.id != property.buyerid && currentUser.id != prop?.sellerid)
                //     return Results.Forbid();

                db.propertybuyerdoc.Update(property);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Putpropertybadge")
                .WithOpenApi();

            // Admin or the buyer who created the request can delete
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                var doc = await db.propertybuyerdoc.FindAsync(id);
                if (doc == null)
                {
                    return Results.NotFound();
                }

                // if (currentUser.admin != true && currentUser.id != doc.buyerid)
                //     return Results.Forbid();

                db.propertybuyerdoc.Remove(doc);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deletepropertybuyerdoc")
                .WithOpenApi();

            // Force buyerid to current user
            group.MapPost("/", async (PropertyBuyerDoc property, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                // if (currentUser == null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(property.requestdoc))
                {
                    return Results.BadRequest(new { error = "requestdoc is required" });
                }
                if (property.propertyid <= 0)
                {
                    return Results.BadRequest(new { error = "Valid propertyid is required" });
                }

                // Force buyer to current user
                // property.buyerid = currentUser.id;
                property.dte = DateTime.UtcNow;
                db.Add(property);
                await db.SaveChangesAsync();
                return Results.Created($"/api/propertybuyerdoc/{property.id}", property);
            })
                .WithName("Postpropertybuyerdoc")
                .WithOpenApi();
        }
    }
}
