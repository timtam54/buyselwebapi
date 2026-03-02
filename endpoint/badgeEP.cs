
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class badgeEP
    {

        
        public static void MapBadgeEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/badge").WithTags(nameof(badgeEP));

            group.MapGet("/", async (int id, dbcontext db) =>
            {
                var sched = await db.badge.ToListAsync();
                return sched;
            })
          .WithName("Getbadge")
          .WithOpenApi();

            
        }
    }

   
}