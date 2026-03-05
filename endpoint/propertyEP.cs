using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Property endpoints: CRUD, search, geocoding, favourites.
    /// Public: browse published properties, search, view single property.
    /// Protected: seller can manage own listings. Admin can view all/audited.
    /// sellerid is forced to current user on create to prevent impersonation.
    /// Auto-geocodes addresses via Google Maps API when lat/lon is missing.
    /// </summary>
    public static class propertyEP
    {
        /// <summary>
        /// Geocodes properties missing lat/lon using Google Maps Geocoding API.
        /// Called on property list endpoints to fill in coordinates lazily.
        /// </summary>
        async static Task<List<Property>> latlon(List<Property> data, dbcontext db, IHttpClientFactory httpFactory)
        {
            foreach (var item in data)
            {
                if (item.id != null)
                {
                    if (item.lon == 0 || item.lon == null || item.lat == 0 || item.lat == null)
                    {
                        var uri = "/maps/api/geocode/json?address=" + item.address.Replace(" ", "~").Replace("#", "") + "~Australia&key=AIzaSyANThwic7Udj0r5ulBoPN9Dp2lSwJKOAK4";
                        var client = httpFactory.CreateClient("GoogleMaps");
                        try
                        {
                            var result = await client.PostAsync(uri, null);
                            if (result.IsSuccessStatusCode)
                            {
                                var xx = await result.Content.ReadAsStringAsync();
                                Root COS = System.Text.Json.JsonSerializer.Deserialize<Root>(xx);
                                if (COS.results.Count() == 0)
                                {
                                    item.lat = 0;
                                    item.lon = 0;
                                }
                                else
                                {
                                    item.lat = COS.results.FirstOrDefault().geometry.location.lat;
                                    item.lon = COS.results.FirstOrDefault().geometry.location.lng;
                                }
                                await db.SaveChangesAsync();
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return data;
        }
        public static void MapPropertyEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/property").WithTags(nameof(propertyEP));

            // Public - view single property
            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.property.Where(i => i.id == id).FirstOrDefaultAsync();
                return sched;
            })
            .AllowAnonymous()
            .WithName("Getproperty")
            .WithOpenApi();

            // Seller can only view their own properties by ID
            group.MapGet("/seller/{id}", async (int id, dbcontext db, IHttpClientFactory httpFactory, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (currentUser.admin != true && currentUser.id != id)
                    return Results.Forbid();

                var sched = await db.property.Where(i => i.sellerid == id).ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                return Results.Ok(AddPic(xx, db));
            })
            .WithName("GetpropertybySeller")
            .WithOpenApi();

            // Public - view seller's listings by email
            group.MapGet("/sellerusername/{id}", async (string id, dbcontext db, IHttpClientFactory httpFactory, ILogger<dbcontext> logger) =>
            {
                var sched = await (from prp in db.property join usr in db.user on prp.sellerid equals usr.id where usr.email == id select prp).ToListAsync();
                var xx = await latlon(sched, db, httpFactory);

                try
                {
                    await auditEP.Audit(id, db, "Buyer/Property", "View my property", 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for {Email}", id);
                }
                return AddPic(xx, db);
            })
            .AllowAnonymous()
            .WithName("GetpropertybySellerUsername")
            .WithOpenApi();


            List<PropertyPic> AddPic(List<Property> prop, dbcontext db)
            {
                var proppic = (from pp in db.propertyphoto where prop.Select(j => j.id).Contains(pp.propertyid) group pp by pp.propertyid into ppid select new IDPIC { id = ppid.Key, pic = ppid.Min(i => i.photobloburl) }).ToList().ToList();
                return (from prp in prop
                        join pp in proppic on prp.id equals pp.id into ppGroup
                        from pp in ppGroup.DefaultIfEmpty()
                        select new PropertyPic
                        {
                            titlesrchcouncilrateazureblob = prp.titlesrchcouncilrateazureblob,
                            titlesrchcouncilrateverified = prp.titlesrchcouncilrateverified,
                            id = prp.id,
                            address = prp.address,
                            dte = prp.dte,
                            lat = prp.lat,
                            lon = prp.lon,
                            photobloburl = pp?.pic,
                            price = prp.price,
                            sellerid = prp.sellerid,
                            title = prp.title,
                            typeofprop = prp.typeofprop,
                            baths = prp.baths,
                            beds = prp.beds,
                            buildyear = prp.buildyear,
                            carspaces = prp.carspaces,
                            landsize = prp.landsize,
                            postcode = prp.postcode,
                            suburb = prp.suburb,
                            state = prp.state,
                            country = prp.country,
                            buildinginspazureblob = prp.buildinginspazureblob,
                            buildinginspverified = prp.buildinginspverified,
                            pestinspazureblob = prp.pestinspazureblob,
                            pestinspverified = prp.pestinspverified,
                            buildinginsppublic = prp.buildinginsppublic,
                            pestinsppublic = prp.pestinsppublic,
                            status = prp.status,
                            titlesrchcouncilratepublic = prp.titlesrchcouncilratepublic,
                            rejectedreason = prp.rejectedreason,
                            contractsale = prp.contractsale,
                            poolcert = prp.poolcert,
                            smokealarm = prp.smokealarm
                        }).ToList();
            }

            // Admin only - view all properties (including unpublished) with audit
            group.MapGet("/audited/{id}", async (string id, dbcontext db, IHttpClientFactory httpFactory, ILogger<dbcontext> logger, ClaimsPrincipal principal) =>
            {
                if (!await AuthHelper.IsAdmin(principal, db))
                    return Results.Forbid();

                var sched = await db.property.ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                try
                {
                    await auditEP.Audit(id, db, "Property", "Full Select", 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for {Email}", id);
                }
                return Results.Ok(AddPic(xx, db));
            })
       .WithName("GetpropertysAudit")
       .WithOpenApi();

            // Public - browse published properties
            group.MapGet("/", async (dbcontext db, IHttpClientFactory httpFactory) =>
            {
                var sched = await db.property
                    .Where(i => i.status == "published")
                    .OrderByDescending(i => i.dte)
                    .ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                return Results.Ok(AddPic(xx, db));
            })
       .AllowAnonymous()
       .WithName("Getpropertys")
       .WithOpenApi();

            // Admin only - view all properties (including draft/rejected)
            group.MapGet("/all/", async (dbcontext db, IHttpClientFactory httpFactory, ClaimsPrincipal principal) =>
            {
                if (!await AuthHelper.IsAdmin(principal, db))
                    return Results.Forbid();

                var sched = await db.property
                    .OrderByDescending(i => i.dte)
                    .ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                return Results.Ok(AddPic(xx, db));
            })
      .WithName("Getpropertysall")
      .WithOpenApi();

            group.MapGet("/favs/{id}", async (string id, dbcontext db, IHttpClientFactory httpFactory, ClaimsPrincipal principal) =>
            {
                // Users can only view their own favourites
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();
                if (currentUser.admin != true && currentUser.email != id)
                    return Results.Forbid();

                var usr = db.user.Where(i => i.email == id).FirstOrDefault();
                if (usr == null)
                    return Results.NotFound();
                var favs = await db.userpropertyfav.Where(i => i.user_id == usr.id).ToListAsync();
                var sched = await db.property.Where(i => i.status == "published" && favs.Select(p => p.property_id).Contains(i.id)).ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                return Results.Ok(AddPic(xx, db));
            })
      .WithName("favpropertys")
      .WithOpenApi();

            // Public - search properties
            group.MapGet("/postsubbedbath/{id}/{bed}/{bath}", async (string id, int beds, int baths, dbcontext db, IHttpClientFactory httpFactory) =>
            {
                var sched = await db.property.Where(i => i.status == "published" && (i.postcode == id || i.suburb == id || id == "~") && (i.beds == beds || beds == 0) && (i.baths == baths || baths == 0)).ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                return AddPic(xx, db);
            })
   .AllowAnonymous()
   .WithName("Searchpropertys")
   .WithOpenApi();

            // Public - search properties with audit
            group.MapGet("/postsubbedbath/{id}/{bed}/{bath}/{user}", async (string id, int beds, int baths, string user, dbcontext db, IHttpClientFactory httpFactory, ILogger<dbcontext> logger) =>
            {
                var sched = await db.property.Where(i => (i.postcode == id || i.suburb == id || id == "~") && (i.beds == beds || beds == 0) && (i.baths == baths || baths == 0)).ToListAsync();
                var xx = await latlon(sched, db, httpFactory);
                try
                {
                    await auditEP.Audit(user, db, "Property", "Search:Bed" + beds.ToString() + ",baths:" + baths.ToString() + ",postcode:" + id, 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for search by {User}", user);
                }
                return AddPic(xx, db);
            })
     .AllowAnonymous()
     .WithName("SearchpropertysAudit")
     .WithOpenApi();

            // Only the property seller or admin can update
            group.MapPut("/", async (Property property, dbcontext db, ClaimsPrincipal principal, ILogger<dbcontext> logger) =>
            {
                if (string.IsNullOrWhiteSpace(property.title) || string.IsNullOrWhiteSpace(property.address))
                {
                    return Results.BadRequest(new { error = "Title and address are required" });
                }

                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                if (currentUser.admin != true && currentUser.id != property.sellerid)
                    return Results.Forbid();

                db.property.Update(property);
                await db.SaveChangesAsync();
                try
                {
                    var seller = await db.user.Where(i => i.id == property.sellerid).FirstOrDefaultAsync();
                    await auditEP.Audit(seller.email, db, "Property", "Seller updated property info", 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for property update {PropertyId}", property.id);
                }
                return Results.NoContent();
            })
                .WithName("Putproperty")
                .WithOpenApi();

            // Only the property seller or admin can delete
            group.MapDelete("/{id}", async (int id, dbcontext db, ClaimsPrincipal principal) =>
            {
                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                var audit = await db.property.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }

                if (currentUser.admin != true && currentUser.id != audit.sellerid)
                    return Results.Forbid();

                db.property.Remove(audit);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deleteproperty")
                .WithOpenApi();

            // Seller must match current user
            group.MapPost("/", async (Property property, dbcontext db, ClaimsPrincipal principal, ILogger<dbcontext> logger) =>
            {
                if (string.IsNullOrWhiteSpace(property.title) || string.IsNullOrWhiteSpace(property.address))
                {
                    return Results.BadRequest(new { error = "Title and address are required" });
                }

                var currentUser = await AuthHelper.GetCurrentUser(principal, db);
                if (currentUser == null) return Results.Unauthorized();

                // Force sellerid to current user (prevent impersonation)
                property.sellerid = currentUser.id;
                property.dte = DateTime.UtcNow;
                db.Add(property);
                await db.SaveChangesAsync();
                try
                {
                    await auditEP.Audit(currentUser.email, db, "Property", "Seller added property" + property.id.ToString(), 0);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to create audit entry for property creation {PropertyId}", property.id);
                }
                return Results.Created($"/api/property/{property.id}", property);
            })
                .WithName("Postproperty")
                .WithOpenApi();
        }
    }
    public class IDPIC
    {
        public int id { get; set; }
        public string pic { get; set; }
    }
    public class AddressComponent
    {
        public string long_name { get; set; }
        public string short_name { get; set; }
        public List<string> types { get; set; }
    }

    public class Geometry
    {
        public Location location { get; set; }
        public string location_type { get; set; }
        public Viewport viewport { get; set; }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Northeast
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class PlusCode
    {
        public string compound_code { get; set; }
        public string global_code { get; set; }
    }

    public class Result
    {
        public List<AddressComponent> address_components { get; set; }
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public bool partial_match { get; set; }
        public string place_id { get; set; }
        public PlusCode plus_code { get; set; }
        public List<string> types { get; set; }
    }

    public class Root
    {
        public List<Result> results { get; set; }
        public string status { get; set; }
    }

    public class Southwest
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Viewport
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }
}
