using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// User property favourite endpoints: users can save/remove favourite properties.
    /// Access: Users can only manage their own favourites. user_id forced on create.
    /// </summary>
    public static class userpropertyfavEP
    {
        public static void MapUserPropertyFavEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/userpropertyfav").WithTags(nameof(userpropertyfavEP));

            // Users can only view their own favourites
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();
                if (currentUser.admin != true && currentUser.id != id)
                    return Results.Forbid();

                var sched = await db.userpropertyfav.Where(i => i.user_id == id).ToListAsync();
                return Results.Ok(sched);
            })
          .WithName("Getuserpropertyfav")
          .WithOpenApi();

            // Users can only delete their own favourites
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var fav = await db.userpropertyfav.FindAsync(id);
                if (fav == null)
                {
                    return Results.NotFound();
                }

                if (currentUser.admin != true && currentUser.id != fav.user_id)
                    return Results.Forbid();

                db.userpropertyfav.Remove(fav);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deleteuserpropertyfav")
                .WithOpenApi();

            // Force user_id to current user
            group.MapPost("/", async (UserPropertyFav fav, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                // Force user_id to current user
                fav.user_id = currentUser.id;
                db.Add(fav);
                await db.SaveChangesAsync();
                return Results.Created($"/api/userpropertyfav/{fav.id}", fav);
            })
                .WithName("Postuserpropertyfav")
                .WithOpenApi();
        }
    }
}
