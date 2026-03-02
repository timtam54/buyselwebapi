
using buyselwebapi.data;
using buyselwebapi.model;
using Microsoft.EntityFrameworkCore;

using System.Net;

namespace IncidentWebAPI.endpoint
{
    public static class propertyEP
    {
        async static Task<List<Property>> latlon(List<Property> data, dbcontext db)
        {
            foreach (var item in data)
            {
                if (item.id != null)
                {
                    if (item.lon == 0 || item.lon == null || item.lat == 0 || item.lat == null)
                    {
                        string Error = "";
                        var uri = "https://maps.googleapis.com/maps/api/geocode/json?address=" + item.address.Replace(" ", "~").Replace("#", "") + "~Australia&key=AIzaSyANThwic7Udj0r5ulBoPN9Dp2lSwJKOAK4";
                        using (var cli = new HttpClient())
                        {
                            var result = await cli.PostAsync(uri, null);
                            if (!result.IsSuccessStatusCode)
                            {
                                Error = result.ReasonPhrase;
                            }
                            else if (result.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Error = "Token invalid - refreshed please try again now";
                            }
                            else
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
                    }
                }
            }
            return data;
        }
        public static void MapPropertyEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/property").WithTags(nameof(propertyEP));

            group.MapGet("/{id}", async (int id, dbcontext db) =>
            {
                var sched = await db.property.Where(i => i.id == id).FirstOrDefaultAsync();
                return sched;
            })
            .WithName("Getproperty")
            .WithOpenApi();

            group.MapGet("/seller/{id}", async (int id,dbcontext db) =>
            {
                var sched = await db.property.Where(i=>i.sellerid==id).ToListAsync();
                var xx= await latlon(sched, db);
                return AddPic(xx,db);
            })
            .WithName("GetpropertybySeller")
            .WithOpenApi();

            group.MapGet("/sellerusername/{id}", async (string id, dbcontext db) =>
            {
                var sched = await (from prp in db.property join usr in db.user on prp.sellerid equals usr.id where usr.email == id select prp).ToListAsync();
                var xx = await latlon(sched, db);
              
                await auditEP.Audit(id, db, "Buyer/Property", "View my property",0);
                return AddPic(xx, db);
            })
            .WithName("GetpropertybySellerUsername")
            .WithOpenApi();


            List<PropertyPic> AddPic(List<Property> prop, dbcontext db)
            {
                var proppic = (from pp in db.propertyphoto where prop.Select(j => j.id).Contains(pp.propertyid) group pp by pp.propertyid into ppid  select new IDPIC { id=ppid.Key, pic=ppid.Min(i=>i.photobloburl) } ).ToList().ToList();
                //return (from prp in prop join pp in proppic on prp.id equals pp.id select new PropertyPic { id=prp.id, address=prp.address, dte=prp.dte, lat=prp.lat, lon=prp.lon, photobloburl=pp.pic, price= prp.price, sellerid=prp.sellerid, title=prp.title }).ToList();
                return (from prp in prop
                        join pp in proppic on prp.id equals pp.id into ppGroup
                        from pp in ppGroup.DefaultIfEmpty()
                        select new PropertyPic
                        { titlesrchcouncilrateazureblob=prp.titlesrchcouncilrateazureblob,
                         titlesrchcouncilrateverified=prp.titlesrchcouncilrateverified,
                            id = prp.id,
                            address = prp.address,
                            dte = prp.dte,
                            lat = prp.lat,
                            lon = prp.lon,
                            photobloburl = pp?.pic,  // Use null-conditional operator
                            price = prp.price,
                            sellerid = prp.sellerid,
                            title = prp.title,
                              typeofprop = prp.typeofprop,
                               baths=prp.baths,
                                beds=prp.beds,
                                 buildyear=prp.buildyear,   
                                  carspaces=prp.carspaces,
                                   landsize=prp.landsize,
                                    postcode=prp.postcode,
                                     suburb=prp.suburb,
                                     state=prp.state,
                                     country=prp.country,
                                      buildinginspazureblob=prp.buildinginspazureblob,
                                       buildinginspverified=prp.buildinginspverified,
                                        pestinspazureblob=prp.pestinspazureblob,
                                         pestinspverified=prp.pestinspverified,
                                          buildinginsppublic=prp.buildinginsppublic,
                                           pestinsppublic=prp.pestinsppublic,
                                            status=prp.status,
                                                 titlesrchcouncilratepublic=prp.titlesrchcouncilratepublic,
                                                  rejectedreason=prp  .rejectedreason,
                                                   contractsale=prp.contractsale,
                            poolcert = prp.poolcert,
                             smokealarm = prp.smokealarm
                        }).ToList();
            }

            group.MapGet("/audited/{id}", async (string id,dbcontext db) =>
            {
                var sched = await db.property.ToListAsync();
                var xx= await latlon(sched, db);
                try
                {
                    await auditEP.Audit(id, db, "Property", "Full Select",0);
                }
                catch (Exception ex)
                {
                }
                return AddPic(xx, db);
            })
       .WithName("GetpropertysAudit")
       .WithOpenApi();

            group.MapGet("/", async (dbcontext db) =>
            {
                var sched = await db.property.Where(i=>i.status=="published").ToListAsync();
                var xx = await latlon(sched, db);
               
                return AddPic(xx, db);
            })
       .WithName("Getpropertys")
       .WithOpenApi();

            group.MapGet("/all/", async (dbcontext db) =>
            {
                var sched = await db.property.ToListAsync();
                var xx = await latlon(sched, db);

                return AddPic(xx, db);
            })
      .WithName("Getpropertysall")
      .WithOpenApi();

            group.MapGet("/favs/{id}", async (string id,dbcontext db) =>
            {
                var usr = db.user.Where(i => i.email == id).FirstOrDefault();
                if (usr == null)
                    return null;
                var favs = await db.userpropertyfav.Where(i => i.user_id == usr.id).ToListAsync();
                var sched = await  db.property.Where(i=> i.status == "published" && favs.Select(p=>p.property_id).Contains(i.id)).ToListAsync();
                var xx = await latlon(sched, db);
                return AddPic(xx, db);
            })
      .WithName("favpropertys")
      .WithOpenApi();

            group.MapGet("/postsubbedbath/{id}/{bed}/{bath}", async (string id, int beds, int baths, dbcontext db) =>
            {
                var sched = await db.property.Where(i => i.status == "published" && (i.postcode == id || i.suburb == id || id == "~") && (i.beds == beds || beds == 0) && (i.baths == baths || baths == 0)).ToListAsync();
                var xx = await latlon(sched, db);
                return AddPic(xx, db);
            })
   .WithName("Searchpropertys")
   .WithOpenApi();

            group.MapGet("/postsubbedbath/{id}/{bed}/{bath}/{user}", async (string id, int beds, int baths,string user, dbcontext db) =>
            {
                var sched = await db.property.Where(i => (i.postcode == id || i.suburb == id || id == "~") && (i.beds == beds || beds == 0) && (i.baths == baths || baths == 0)).ToListAsync();
                var xx = await latlon(sched, db);
                try
                {
                    await auditEP.Audit(user, db, "Property", "Search:Bed"+beds.ToString()+",baths:"+baths.ToString()+",postcode:"+id,0);
                }
                catch (Exception ex)
                {
                }
                return AddPic(xx, db);
            })
     .WithName("SearchpropertysAudit")
     .WithOpenApi();
            

            group.MapPut("/", async (Property property, dbcontext db) =>
            {
                db.property.Update(property);
                await db.SaveChangesAsync();
                try
                {
                    var seller = await db.user.Where(i => i.id == property.sellerid).FirstOrDefaultAsync();
                    await auditEP.Audit(seller.email, db, "Property", "Seller updated property info",0);
                }
                catch (Exception ex)
                {
                }
                return Results.NoContent();
            })
                .WithName("Putproperty")
                .WithOpenApi();

            group.MapDelete("/{id}", async (int id, dbcontext db) =>
            {
                var audit = await db.property.FindAsync(id);
                if (audit == null)
                {
                    return Results.NotFound();
                }
                db.property.Remove(audit);
               /* try
                {
                    var seller = await db.user.Where(i => i.id == audit.sellerid).FirstOrDefaultAsync();
                    await auditEP.Audit(seller.email, db, "Property", "Seller deleted property"+audit.id.ToString());
                }
                catch (Exception ex)
                {
                }*/
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
                .WithName("Deleteproperty")
                .WithOpenApi();

            group.MapPost("/", async (Property property, dbcontext db) =>
            {
                db.Add(property);
                property.dte = DateTime.UtcNow.AddHours(10);
                await db.SaveChangesAsync();
                try
                {
                    var seller = await db.user.Where(i => i.id == property.sellerid).FirstOrDefaultAsync();
                    await auditEP.Audit(seller.email, db, "Property", "Seller added property" + property.id.ToString(),0);
                }
                catch (Exception ex)
                {
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