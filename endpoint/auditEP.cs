
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class auditEP
    {
        public static async Task Audit(string id, dbcontext db, string page, string action, int? propertyid)
        {
            Audit aud = new Audit();
            aud.propertyid=propertyid;
            aud.id = 0;
            aud.page = page;
            aud.dte = DateTime.Now;
            aud.action = action;
            aud.username = id;
            try
            {
                aud.ipaddress = (await db.audit.Where(i => i.username == id).OrderByDescending(i => i.dte).Take(1).FirstOrDefaultAsync()).ipaddress;
            }
            catch (Exception ex)
            {

            }
            db.Add(aud);
            await db.SaveChangesAsync();
        }

        public static void MapAuditEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/audit").WithTags(nameof(auditEP));

          

            group.MapGet("/clearaudit", async (dbcontext db) =>
            {
                await db.Database.ExecuteSqlAsync($"exec clearaudit");
                return;
            })
       .WithName("clearaudit")
       .WithOpenApi();

           group.MapGet("/summary/", async (dbcontext db) =>
            {
                var sched = await db.audsummary.FromSqlRaw($"exec companyauditsummary").ToListAsync();
                return sched;
            })
         .WithName("Getauditsum")
         .WithOpenApi();

            group.MapGet("/", async (dbcontext db) =>
            {
                // var sched = await (from au in db.audit join us in db.user on au.username equals us.username join si in db.companysite on us.companyid equals si.id join co in db.company on si.companyid equals co.id orderby au.dte descending select new audco { username=au.username, company=co.companyname, action=au.action, page=au.page, dte=au.dte, id=au.id } ).Take(300).ToListAsync();
                var sched = await db.audit.OrderByDescending(i => i.dte).Take(300).ToListAsync();
                return sched;
            })
            .WithName("Getaudit")
            .WithOpenApi();

            group.MapGet("/{id}", async (int id,dbcontext db) =>
            {
                // var sched = await (from au in db.audit join us in db.user on au.username equals us.username join si in db.companysite on us.companyid equals si.id join co in db.company on si.companyid equals co.id orderby au.dte descending select new audco { username=au.username, company=co.companyname, action=au.action, page=au.page, dte=au.dte, id=au.id } ).Take(300).ToListAsync();
                var sched = await db.audit.Where(i=>i.propertyid==id).OrderByDescending(i => i.dte).Take(300).ToListAsync();
                return sched;
            })
          .WithName("Getauditprop")
          .WithOpenApi();

         /*   group.MapPut("/", async (Audit audit, dbcontext db) =>
            {
                db.audit.Update(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Putaudit")
                .WithOpenApi();*/

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var audit = await db.audit.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }
                db.audit.Remove(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deleteaudit")
                .WithOpenApi();

            group.MapPost("/", async (Audit audit, dbcontext db) =>
            {
                db.Add(audit);
                audit.dte = DateTime.UtcNow.AddHours(10);

                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Postaudit")
                .WithOpenApi();
        }
    }

   
}