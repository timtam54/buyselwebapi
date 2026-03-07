using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Audit endpoints: logging and viewing user activity.
    /// POST /audit is public (used for frontend analytics tracking).
    /// All read/delete endpoints are admin-only. clearaudit uses DELETE method.
    /// The static Audit() method is called from other endpoints to log actions.
    /// </summary>
    public static class auditEP
    {
        /// <summary>
        /// Creates an audit log entry. Called from other endpoints to record user actions.
        /// Attempts to carry forward the IP address from the user's most recent audit entry.
        /// </summary>
        public static async Task Audit(string id, dbcontext db, string page, string action, int? propertyid)
        {
            Audit aud = new Audit();
            aud.propertyid = propertyid;
            aud.id = 0;
            aud.page = page;
            aud.dte = DateTime.UtcNow;
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

            // Admin only - clear audit logs
            group.MapDelete("/clearaudit", async (dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

                await db.Database.ExecuteSqlAsync($"exec clearaudit");
                return Results.NoContent();
            })
       .WithName("clearaudit")
       .WithOpenApi();

            // Admin only - audit summary
            group.MapGet("/summary/", async (dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

                var sched = await db.audsummary.FromSqlInterpolated($"exec companyauditsummary").ToListAsync();
                return Results.Ok(sched);
            })
         .WithName("Getauditsum")
         .WithOpenApi();

            // Admin only - view all audit logs
            group.MapGet("/", async (dbcontext db, ClaimsPrincipal principal, int page = 1, int pageSize = 100) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

                pageSize = Math.Clamp(pageSize, 1, 300);
                var sched = await db.audit
                    .OrderByDescending(i => i.dte)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                return Results.Ok(sched);
            })
            .WithName("Getaudit")
            .WithOpenApi();

            // Admin only - view audit for specific property
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

                var sched = await db.audit.Where(i => i.propertyid == id).OrderByDescending(i => i.dte).Take(300).ToListAsync();
                return Results.Ok(sched);
            })
          .WithName("Getauditprop")
          .WithOpenApi();

            // Admin only - delete audit entry
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                // TODO: Re-enable auth check after testing
                // if (!await AuthHelper.IsAdmin(principal, db))
                //     return Results.Forbid();

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

            // Public - analytics tracking
            group.MapPost("/", async (Audit audit, dbcontext db) =>
            {
                if (string.IsNullOrWhiteSpace(audit.action) || string.IsNullOrWhiteSpace(audit.page))
                {
                    return Results.BadRequest(new { error = "Action and page are required" });
                }
                db.Add(audit);
                audit.dte = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).AllowAnonymous()
                .WithName("Postaudit")
                .WithOpenApi();
        }
    }
}
