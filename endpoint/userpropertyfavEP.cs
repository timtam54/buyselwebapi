
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class userpropertyfavEP
    {
       

        public static void MapUserPropertyFavEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/userpropertyfav").WithTags(nameof(userpropertyfavEP));



            group.MapGet("/{id}", async (int id,dbcontext db) =>
            {
                // var sched = await (from au in db.audit join us in db.user on au.username equals us.username join si in db.companysite on us.companyid equals si.id join co in db.company on si.companyid equals co.id orderby au.dte descending select new audco { username=au.username, company=co.companyname, action=au.action, page=au.page, dte=au.dte, id=au.id } ).Take(300).ToListAsync();
                var sched = await db.userpropertyfav.Where(i=>i.user_id==id).ToListAsync();
                return sched;
            })
          .WithName("Getuserpropertyfav")
          .WithOpenApi();

      

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var audit = await db.userpropertyfav.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }
                db.userpropertyfav.Remove(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deleteuserpropertyfav")
                .WithOpenApi();

            group.MapPost("/", async (UserPropertyFav audit, dbcontext db) =>
            {
                db.Add(audit);
                //audit.dte = DateTime.UtcNow.AddHours(10);

                await db.SaveChangesAsync();
                return Results.Created($"/api/userpropertyfav/{audit.id}", audit);
            })
                .WithName("Postuserpropertyfav")
                .WithOpenApi();
        }
    }

   
}