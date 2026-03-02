
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class propertyphotoEP
    {

        
        public static void MapPropertyPhotoEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/propertyphoto").WithTags(nameof(propertyphotoEP));

            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.propertyphoto.Where(i => i.propertyid == id && (i.doc==null || i.doc==false)).ToListAsync();
                return sched;
            })
          .WithName("Getpropertyphoto")
          .WithOpenApi();

            group.MapGet("/docs/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.propertyphoto.Where(i => i.propertyid == id && i.doc==true).ToListAsync();
                return sched;
            })
         .WithName("Getpropertydocs")
         .WithOpenApi();

            group.MapPut("/", async (PropertyPhoto property, dbcontext db) =>
            {
                db.propertyphoto.Update(property);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Putpropertyphoto")
                .WithOpenApi();

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var audit = await db.propertyphoto.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }
                db.propertyphoto.Remove(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deletepropertyphoto")
                .WithOpenApi();

            group.MapPost("/", async (PropertyPhoto property, dbcontext db) =>
            {
                db.Add(property);
                property.dte = DateTime.UtcNow.AddHours(10);
                await db.SaveChangesAsync();
                try
                {
                    var prop = await db.property.Where(i => i.id == property.propertyid).FirstOrDefaultAsync();
                    var seller = await db.user.Where(i => i.id == prop.sellerid).FirstOrDefaultAsync();
                    await auditEP.Audit(seller.email, db, "PropertyPhoto", "Seller added property photo " + property.id.ToString(),0);
                }
                catch (Exception ex)
                {
                }
                return Results.Created($"/api/propertyphoto/{property.id}", property);
            })
                .WithName("Postpropertyphoto")
                .WithOpenApi();
        }
    }

}