
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class propertybuyerdocEP
    {

       
        public static void MapPropertyBuyerDocEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/propertybuyerdoc").WithTags(nameof(propertybuyerdocEP));

            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.propertybuyerdoc.Where(i => i.propertyid == id).FirstOrDefaultAsync();
                return sched;
            })
          .WithName("Getpropertybuyerdoc")
          .WithOpenApi();

            group.MapGet("/", async (dbcontext db) =>
            {
                var sched = await db.propertybuyerdoc.Where(i => i.action ==null).ToListAsync();
                return sched;
            })
       .WithName("Getpropertybuyerdocoutstand")
       .WithOpenApi();

            group.MapGet("/all/", async ( dbcontext db) =>
            {
                var sched = await db.propertybuyerdoc.ToListAsync();
                return sched;
            })
       .WithName("Getpropertybuyerdocall")
       .WithOpenApi();

            group.MapPut("/", async (PropertyBuyerDoc property, dbcontext db) =>
            {
                db.propertybuyerdoc.Update(property);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Putpropertybadge")
                .WithOpenApi();

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var audit = await db.propertybuyerdoc.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }
                db.propertybuyerdoc.Remove(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deletepropertybuyerdoc")
                .WithOpenApi();

            group.MapPost("/", async (PropertyBuyerDoc property, dbcontext db) =>
            {
                db.Add(property);
                property.dte = DateTime.UtcNow.AddHours(10);
                await db.SaveChangesAsync();
                return Results.Created($"/api/propertybuydoc/{property.id}", property);
            })
                .WithName("Postpropertybuyerdoc")
                .WithOpenApi();
        }
    }

}