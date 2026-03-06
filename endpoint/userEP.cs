using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// User endpoints: OAuth login, registration, profile CRUD.
    /// Access: Users can only view/update their own profile. Admin can manage all users.
    /// New users cannot register as admin. Admin flag cannot be self-promoted.
    /// </summary>
    public static class userEP
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/user").WithTags(nameof(userEP));

            routes.MapPost("/api/users/oauth", async (OAuthUserRequest request, dbcontext db, ILogger<dbcontext> logger) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(request.Email) ||
                        string.IsNullOrWhiteSpace(request.Name) ||
                        string.IsNullOrWhiteSpace(request.Provider) ||
                        string.IsNullOrWhiteSpace(request.ProviderId))
                    {
                        return Results.BadRequest(new { error = "Email, Name, Provider, and ProviderId are required" });
                    }

                    var existingUser = await db.user
                        .FirstOrDefaultAsync(u => u.email == request.Email);

                    if (existingUser != null)
                    {
                        existingUser.firstname = request.Name.Split(' ').FirstOrDefault() ?? request.Name;
                        existingUser.lastname = request.Name.Split(' ').Skip(1).FirstOrDefault() ?? "";

                        await db.SaveChangesAsync();

                        return Results.Ok(new
                        {
                            id = existingUser.id.ToString(),
                            email = existingUser.email,
                            name = $"{existingUser.firstname} {existingUser.lastname}".Trim(),
                            picture = existingUser.photoazurebloburl,
                            role = "user",
                            createdAt = existingUser.dte
                        });
                    }
                    else
                    {
                        var nameParts = request.Name.Split(' ', 2);
                        var newUser = new User
                        {
                            email = request.Email,
                            firstname = nameParts.FirstOrDefault() ?? request.Name,
                            lastname = nameParts.Length > 1 ? nameParts[1] : "",
                            photoazurebloburl = null,
                            mobile = "",
                            address = "",
                            residencystatus = "",
                            maritalstatus = null,
                            powerofattorney = "",
                            termsconditions = false,
                            privacypolicy = false,
                            idtype = "none",
                            idbloburl = "",
                            idverified = null,
                            ratesnotice = null,
                            titlesearch = null,
                            ratesnoticeverified = null,
                            titlesearchverified = null,
                            photoverified = null,
                            dte = DateTime.UtcNow
                        };

                        db.user.Add(newUser);
                        await db.SaveChangesAsync();

                        return Results.Ok(new
                        {
                            id = newUser.id.ToString(),
                            email = newUser.email,
                            name = $"{newUser.firstname} {newUser.lastname}".Trim(),
                            picture = newUser.photoazurebloburl,
                            role = "user",
                            createdAt = newUser.dte
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in OAuth user creation for {Email}", request.Email);
                    return Results.Problem(
                        detail: "An unexpected error occurred",
                        statusCode: 500,
                        title: "Error creating/updating user"
                    );
                }
            })
           .AllowAnonymous()
           .RequireRateLimiting("auth")
           .WithName("CreateOrUpdateOAuthUser")
           .WithTags("Authentication");

            // Users can only view their own profile, admins can view any
            group.MapGet("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (currentUser.admin != true && currentUser.id != id)
                    return Results.Forbid();

                var sched = await db.user.Where(i => i.id == id).FirstOrDefaultAsync();
                return sched is not null ? Results.Ok(sched) : Results.NotFound();
            })
          .WithName("Getuser")
          .WithOpenApi();

            // Users can only look up their own email, admins can look up any
            group.MapGet("/email/{id}", async (string id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (currentUser.admin != true && currentUser.email != id)
                    return Results.Forbid();

                var sched = await db.user.Where(i => i.email == id).FirstOrDefaultAsync();
                return sched is not null ? Results.Ok(sched) : Results.NotFound();
            })
        .WithName("Getuseremail")
        .WithOpenApi();

            // Admin only - list all users
            group.MapGet("/", async (dbcontext db, ClaimsPrincipal principal, int page = 1, int pageSize = 50) =>
            {
                if (!await AuthHelper.IsAdmin(principal, db))
                    return Results.Forbid();

                pageSize = Math.Clamp(pageSize, 1, 100);
                var sched = await db.user
                    .OrderByDescending(i => i.dte)  
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                return Results.Ok(sched);
            })
       .WithName("Getusers")
       .WithOpenApi();

            group.MapGet("/sellers/", async (dbcontext db) =>
            {
                var sched = await db.user.Where(i => db.property.Select(i => i.sellerid).Contains(i.id)).ToListAsync();
                return sched;
            })
     .WithName("GetSellers")
     .WithOpenApi();

            // Users can only update their own profile, admin flag cannot be self-set
            group.MapPut("/", async (User user, dbcontext db, ClaimsPrincipal principal, ILogger<dbcontext> logger) =>
            {
                if (string.IsNullOrWhiteSpace(user.email))
                {
                    return Results.BadRequest(new { error = "Email is required" });
                }

                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                // Non-admins can only update themselves
                if (currentUser.admin != true && currentUser.id != user.id)
                    return Results.Forbid();

                // Non-admins cannot promote themselves to admin
                if (currentUser.admin != true && user.admin == true)
                    return Results.Forbid();

                db.user.Update(user);
                await db.SaveChangesAsync();
                try
                {
                    await auditEP.Audit(user.email, db, "User Details", "User updated details", 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for user update {Email}", user.email);
                }
                return Results.NoContent();
            })
                .WithName("Putuser")
                .WithOpenApi();

            // Admin only - delete users
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                if (!await AuthHelper.IsAdmin(principal, db))
                    return Results.Forbid();

                var audit = await db.user.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }
                db.user.Remove(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deleteuser")
                .WithOpenApi();

            group.MapPost("/", async (User property, dbcontext db, ILogger<dbcontext> logger) =>
            {
                if (string.IsNullOrWhiteSpace(property.email))
                {
                    return Results.BadRequest(new { error = "Email is required" });
                }
                // New users cannot register as admin
                property.admin = false;
                db.Add(property);
                property.dte = DateTime.UtcNow;
                await db.SaveChangesAsync();
                try
                {
                    await auditEP.Audit(property.email, db, "User Signup", "New User sign up", 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for user signup {Email}", property.email);
                }
                return Results.Created($"/api/user/{property.id}", property);
            })
                .AllowAnonymous()
                .RequireRateLimiting("auth")
                .WithName("Postuser")
                .WithOpenApi();
        }
    }
}
