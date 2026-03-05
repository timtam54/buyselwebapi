using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Badge endpoints: read-only listing of all available badges.
    /// Requires authentication. No write operations.
    /// </summary>
    public static class badgeEP
    {
        public static void MapBadgeEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/badge").WithTags(nameof(badgeEP));

            group.MapGet("/", async (dbcontext db) =>
            {
                var sched = await db.badge.ToListAsync();
                return sched;
            })
          .WithName("Getbadge")
          .WithOpenApi();
        }
    }
}
