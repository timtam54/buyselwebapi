using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Property photo/document endpoints.
    /// GET endpoints are open to authenticated users. All mutations (POST/PUT/DELETE)
    /// require the current user to be the property seller or admin.
    /// Photos vs documents distinguished by the "doc" boolean field.
    /// </summary>
    public static class propertyphotoEP
    {
        public static void MapPropertyPhotoEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/propertyphoto").WithTags(nameof(propertyphotoEP));

            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.propertyphoto.Where(i => i.propertyid == id && (i.doc == null || i.doc == false)).ToListAsync();
                return sched;
            })
          .WithName("Getpropertyphoto")
          .WithOpenApi();

            group.MapGet("/docs/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.propertyphoto.Where(i => i.propertyid == id && i.doc == true).ToListAsync();
                return sched;
            })
         .WithName("Getpropertydocs")
         .WithOpenApi();

            // Only the property seller can update photos
            group.MapPut("/", async (PropertyPhoto property, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var prop = await db.property.FindAsync(property.propertyid);
                if (prop == null) return Results.NotFound();
                if (currentUser.admin != true && currentUser.id != prop.sellerid)
                    return Results.Forbid();

                db.propertyphoto.Update(property);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Putpropertyphoto")
                .WithOpenApi();

            // Only the property seller can delete photos
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var photo = await db.propertyphoto.FindAsync(id);
                if (photo == null)
                {
                    return Results.NotFound();
                }

                var prop = await db.property.FindAsync(photo.propertyid);
                if (currentUser.admin != true && currentUser.id != prop?.sellerid)
                    return Results.Forbid();

                db.propertyphoto.Remove(photo);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deletepropertyphoto")
                .WithOpenApi();

            // Only the property seller can add photos
            group.MapPost("/", async (PropertyPhoto property, dbcontext db, ClaimsPrincipal principal, ILogger<dbcontext> logger) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(property.photobloburl))
                {
                    return Results.BadRequest(new { error = "photobloburl is required" });
                }
                if (property.propertyid <= 0)
                {
                    return Results.BadRequest(new { error = "Valid propertyid is required" });
                }

                var prop = await db.property.FindAsync(property.propertyid);
                if (prop == null) return Results.NotFound();
                if (currentUser.admin != true && currentUser.id != prop.sellerid)
                    return Results.Forbid();

                db.Add(property);
                property.dte = DateTime.UtcNow;
                await db.SaveChangesAsync();
                try
                {
                    await auditEP.Audit(currentUser.email, db, "PropertyPhoto", "Seller added property photo " + property.id.ToString(), 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for photo upload {PhotoId}", property.id);
                }
                return Results.Created($"/api/propertyphoto/{property.id}", property);
            })
                .WithName("Postpropertyphoto")
                .WithOpenApi();
        }
    }
}
