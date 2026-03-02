
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;

namespace IncidentWebAPI.endpoint
{
    public static class userEP
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/user").WithTags(nameof(userEP));

            routes.MapPost("/api/users/oauth", async (OAuthUserRequest request, dbcontext db) =>
            {
                try
                {
                    // Validate request
                    if (string.IsNullOrWhiteSpace(request.Email) ||
                        string.IsNullOrWhiteSpace(request.Name) ||
                        string.IsNullOrWhiteSpace(request.Provider) ||
                        string.IsNullOrWhiteSpace(request.ProviderId))
                    {
                        return Results.BadRequest(new { error = "Email, Name, Provider, and ProviderId are required" });
                    }

                    // Check if user already exists
                    var existingUser = await db.user
                        .FirstOrDefaultAsync(u => u.email == request.Email);

                    if (existingUser != null)
                    {
                        // Update existing user
                        existingUser.firstname = request.Name.Split(' ').FirstOrDefault() ?? request.Name;
                        existingUser.lastname = request.Name.Split(' ').Skip(1).FirstOrDefault() ?? "";

                        //if (!string.IsNullOrEmpty(request.Picture))
                        //{
                        //  todotim   existingUser.photoazurebloburl = request.Picture;
                        //}

                        await db.SaveChangesAsync();

                        return Results.Ok(new
                        {
                            id = existingUser.id.ToString(),
                            email = existingUser.email,
                            name = $"{existingUser.firstname} {existingUser.lastname}".Trim(),
                            picture = existingUser.photoazurebloburl,//todotim 
                            role = "user", // You can implement role logic here
                            createdAt = existingUser.dte
                        });
                    }
                    else
                    {
                        // Create new user
                        var nameParts = request.Name.Split(' ', 2);
                        var newUser = new User
                        {
                            email = request.Email,
                            firstname = nameParts.FirstOrDefault() ?? request.Name,
                            lastname = nameParts.Length > 1 ? nameParts[1] : "",
                            photoazurebloburl = null,//todotim request.Picture ?? "",
                            mobile = "", // Will be filled in profile completion
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
                            picture =newUser.photoazurebloburl,
                            //todotim
                            role = "user",
                            createdAt = newUser.dte
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in OAuth user creation: {ex.Message}");
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: 500,
                        title: "Error creating/updating user"
                    );
                }
            })
           .WithName("CreateOrUpdateOAuthUser")
           .WithTags("Authentication");

            group.MapGet("/{id}", async (int id,dbcontext db) =>
            {
                var sched = await db.user.Where(i => i.id==id).FirstOrDefaultAsync();
                return sched;
            })
          .WithName("Getuser")
          .WithOpenApi();

            group.MapGet("/email/{id}", async (string id, dbcontext db) =>
            {
                var sched = await db.user.Where(i => i.email == id).FirstOrDefaultAsync();
                return sched;
            })
        .WithName("Getuseremail")
        .WithOpenApi();

            group.MapGet("/", async ( dbcontext db) =>
            {
                var sched = await db.user.ToListAsync();
                return sched;
            })
       .WithName("Getusers")
       .WithOpenApi();

            group.MapGet("/sellers/", async (dbcontext db) =>
            {
                var sched = await db.user.Where(i=>db.property.Select(i=>i.sellerid).Contains(i.id)).ToListAsync();
                return sched;
            })
     .WithName("GetSellers")
     .WithOpenApi();

            group.MapPut("/", async (User user, dbcontext db) =>
            {
                db.user.Update(user);
                await db.SaveChangesAsync();
                try
                {
                    await auditEP.Audit(user.email, db, "User Details", "User updated details",0);
                }
                catch (Exception ex)
                {
                }
                return Results.NoContent();
            })
                .WithName("Putuser")
                .WithOpenApi();

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
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

            group.MapPost("/", async (User property, dbcontext db) =>
            {
                db.Add(property);
                property.dte = DateTime.UtcNow.AddHours(10);        
                await db.SaveChangesAsync();
                try
                {
                     await auditEP.Audit(property.email, db, "User Signup", "New User sign up",0);
                }
                catch (Exception ex)
                {
                }
                return Results.Created($"/api/user/{property.id}", property);
            })
                .WithName("Postuser")
                .WithOpenApi();
        }
    }
}