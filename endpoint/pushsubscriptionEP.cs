using buyselwebapi.data;
using dotAPNS;

using Microsoft.EntityFrameworkCore;

using WebPush;


namespace IncidentWebAPI.endpoint
{

    public static class pushsubscriptionEP
    {

        public static void MapPushSubscrptionEndpoints(this IEndpointRouteBuilder routes)
        {

            var group = routes.MapGroup("/api/push").WithTags(nameof(pushsubscriptionEP));

            group.MapPost("/unsubscribe-native", async (
      HttpContext context,
      dbcontext db) =>
            {
                var body = await context.Request.ReadFromJsonAsync<NativeUnsubscribeRequest>();

                if (string.IsNullOrEmpty(body?.email))
                {
                    return Results.BadRequest(new { error = "Missing required fields" });
                }

                var subscriptions = await db.pushsubscriptions
                    .Where(s => s.email == body.email && s.subscriptiontype == "native")
                    .ToListAsync();

                db.pushsubscriptions.RemoveRange(subscriptions);
                await db.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Device unregistered successfully" });
            });



            group.MapPost("/subscribe-native", async (
     HttpContext context,
     dbcontext db) =>
            {
                var body = await context.Request.ReadFromJsonAsync<NativeSubscriptionRequest>();

                if (string.IsNullOrEmpty(body?.token) || string.IsNullOrEmpty(body?.email))
                {
                    return Results.BadRequest(new { error = "Missing required fields" });
                }

                var existing = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s =>
                        s.devicetoken == body.token &&
                        s.email == body.email);

                if (existing != null)
                {
                    existing.lastUsedat = DateTime.UtcNow;
                }
                else
                {
                    db.pushsubscriptions.Add(new buyselwebapi.model.PushSubscription
                    {
                        email = body.email,
                        devicetoken = body.token,
                        platform = body.platform,
                        subscriptiontype = "native",
                        createdat = DateTime.UtcNow,
                        lastUsedat = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, message = "Device registered successfully" });
            });

            group.MapPost("/subscribe", async (buyselwebapi.model.PushSubscription request, dbcontext db) =>
            {
                // Check if endpoint already exists
                var existing = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s => s.endpoint == request.endpoint);

                if (existing != null)
                {
                    // Update existing
                    existing.email = request.email;
                    existing.p256dh = request.p256dh;
                    existing.auth = request.auth;
                    existing.lastUsedat = DateTime.UtcNow;
                }
                else
                {
                    // Create new
                    db.pushsubscriptions.Add(new buyselwebapi.model.PushSubscription
                    {
                        email = request.email,
                        endpoint = request.endpoint,
                        p256dh = request.p256dh,
                        auth = request.auth,
                        createdat = DateTime.UtcNow,
                        lastUsedat = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { success = true });
            });

            // 2.MapPost - Unsubscribe(Delete subscription)

            group.MapPost("/unsubscribe", async (buyselwebapi.model.PushSubscription request, dbcontext db) =>
            {
                var subscription = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s => s.email == request.email && s.endpoint == request.endpoint);

                if (subscription != null)
                {
                    db.pushsubscriptions.Remove(subscription);
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new { success = true });
            });



         

            async Task<int> SendIOSPush(buyselwebapi.model.PushSubscription sub, List<string> alerts, IConfiguration config)
            {
                try
                {
                    // Check if we have a device token
                    if (string.IsNullOrEmpty(sub.devicetoken))
                    {
                        Console.WriteLine($"[SendIOSPush] No device token for {sub.email}");
                        return 0;
                    }
                    var p8FileContents = "MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg5OBXHYd8kzhBlg59\r\nNEdRMxZP0YL4oOscO8ufQc6ZcligCgYIKoZIzj0DAQehRANCAAQaxFX54WT09149\r\nQdQ7oW1QFEs3ZbunPYBqCVC1XpuGTCg72Tqqv/Iaptboe6NbmHYp8wpqKjixTUu3\r\neYoIsHqD";// await File.ReadAllTextAsync(config["APNS_P8_FILE_PATH"]);


                    var options = new ApnsJwtOptions
                    {
                        BundleId = "com.jobsafepro.app",
                        KeyId = "QXVJA2JD43",
                        TeamId = "B25XZGD4VW",
                        CertContent = p8FileContents
                    };

                    var apns = ApnsClient.CreateUsingJwt(
                        new HttpClient(),
                        options

                    );
                    //  apns.UseSandbox();

                    var push = new ApplePush(ApplePushType.Alert)
                        .AddAlert("Job Safe Pro - Action Required", string.Join(", ", alerts))
                        .AddBadge(alerts.Count)
                        .AddSound("default")
                        .AddToken(sub.devicetoken);  // <-- THIS WAS MISSING!

                    Console.WriteLine($"[SendIOSPush] Sending to device token: {sub.devicetoken?.Substring(0, 10)}...");

                    var response = await apns.Send(push);  // <-- Also pass token to Send

                    Console.WriteLine($"[SendIOSPush] Response: IsSuccessful={response.IsSuccessful}, Reason={response.Reason}");

                    return response.IsSuccessful ? 1 : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SendIOSPush] Error: {ex.Message}");
                    return 0;
                }
            }

            async Task<int> SendIOSPushOLD(buyselwebapi.model.PushSubscription sub, List<string> alerts, IConfiguration config)
            {
                try
                {
                    var p8FileContents = "MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg5OBXHYd8kzhBlg59\r\nNEdRMxZP0YL4oOscO8ufQc6ZcligCgYIKoZIzj0DAQehRANCAAQaxFX54WT09149\r\nQdQ7oW1QFEs3ZbunPYBqCVC1XpuGTCg72Tqqv/Iaptboe6NbmHYp8wpqKjixTUu3\r\neYoIsHqD";// await File.ReadAllTextAsync(config["APNS_P8_FILE_PATH"]);

                    var options = new ApnsJwtOptions
                    {
                        BundleId = "com.jobsafepro.app",
                        KeyId = "QXVJA2JD43",
                        TeamId = "B25XZGD4VW",
                        CertContent = p8FileContents
                    };

                    var apns = ApnsClient.CreateUsingJwt(
                        new HttpClient(),
                        options
                    );

                    var push = new ApplePush(ApplePushType.Alert)
                        .AddAlert("Job Safe Pro - Action Required", string.Join(", ", alerts))
                        .AddBadge(alerts.Count)
                        .AddSound("default");

                    var response = await apns.Send(push);
                    return response.IsSuccessful ? 1 : 0;
                }
                catch
                {
                    return 0;
                }
            }

            //    5.MapGet - Get Subscriptions(Optional - for debugging)

            group.MapGet("/subscriptions/{email}", async (string email, dbcontext db) =>
            {
                var subscriptions = await db.pushsubscriptions
                    .Where(s => s.email == email)
                    .Select(s => new
                    {
                        s.id,
                        s.endpoint,
                        s.createdat,
                        s.lastUsedat
                    })
                    .ToListAsync();

                return Results.Ok(subscriptions);
            });


            group.MapPost("/push_subscription", async (HttpContext context, dbcontext db) =>
            {
                try
                {
                    Console.WriteLine("[PushSubscription] Request received");

                    var body = await context.Request.ReadFromJsonAsync<WebPushSubscriptionRequest>();
                    Console.WriteLine($"[PushSubscription] Body deserialized: email={body?.email},endpoint ={ body?.subscription_data?.endpoint?.Substring(0, 50)}...");
          
          if (string.IsNullOrEmpty(body?.email) || body?.subscription_data == null)
                    {
                        Console.WriteLine("[PushSubscription] Validation failed: missing email or subscription_data");
                        return Results.BadRequest(new { error = "Invalid subscription data" });
                    }

                    Console.WriteLine($"[PushSubscription] Checking for existing subscription for endpoint");

                    // Check if subscription already exists for this endpoint (endpoint has unique index)
                    var existing = await db.pushsubscriptions
                        .FirstOrDefaultAsync(s => s.endpoint == body.subscription_data.endpoint);  // ✅ Only check endpoint

                    if (existing != null)
                    {
                        Console.WriteLine($"[PushSubscription] Found existing subscription, updating ID: {existing.id}");
                        // Update existing subscription (including email in case user changed accounts)
                        existing.email = body.email;  // ✅ Update email too
                        existing.p256dh = body.subscription_data.keys?.p256dh;
                        existing.auth = body.subscription_data.keys?.auth;
                        existing.lastUsedat = DateTime.UtcNow;
                        existing.subscriptiontype = "web-push";
                        existing.platform = body.platform ?? "web";  // ✅ Update platform too
                    }
                    else
                    {
                        Console.WriteLine("[PushSubscription] Creating new subscription");
                        // Create new subscription
                        db.pushsubscriptions.Add(new buyselwebapi.model.PushSubscription
                        {
                            email = body.email,
                            endpoint = body.subscription_data.endpoint,
                            p256dh = body.subscription_data.keys?.p256dh,
                            auth = body.subscription_data.keys?.auth,
                            subscriptiontype = "web-push",
                            platform = body.platform ?? "web",
                            createdat = DateTime.UtcNow,
                            lastUsedat = DateTime.UtcNow
                        });
                    }

                    Console.WriteLine("[PushSubscription] Saving changes to database...");
                    await db.SaveChangesAsync();
                    Console.WriteLine("[PushSubscription] Changes saved successfully");

                    return Results.Ok(new { success = true, message = "Subscription saved" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PushSubscription] ERROR: {ex.Message}");
                    Console.WriteLine($"[PushSubscription] Stack trace: {ex.StackTrace}");
                    Console.WriteLine($"[PushSubscription] Inner exception: {ex.InnerException?.Message}");

                    return Results.Json(
                        new
                        {
                            error = "Failed to save subscription",
                            message = ex.Message,
                            innerError = ex.InnerException?.Message,
                            type = ex.GetType().Name
                        },
                        statusCode: 500
                    );
                }
            });

            /////new
            ///

            // POST: /api/push_subscription - Create/Update web push subscription
            group.MapPost("/push_subscriptionOLD", async (HttpContext context, dbcontext db) =>
            {
                var body = await context.Request.ReadFromJsonAsync<WebPushSubscriptionRequest>();

                if (string.IsNullOrEmpty(body?.email) || body?.subscription_data == null)
                {
                    return Results.BadRequest(new { error = "Invalid subscription data" });
                }

                // Check if subscription already exists for this endpoint
                var existing = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s =>
                        s.email == body.email &&
                        s.endpoint == body.subscription_data.endpoint);

                if (existing != null)
                {
                    // Update existing subscription
                    existing.p256dh = body.subscription_data.keys?.p256dh;
                    existing.auth = body.subscription_data.keys?.auth;
                    existing.lastUsedat = DateTime.UtcNow;
                    existing.subscriptiontype = "web-push";
                }
                else
                {
                    // Create new subscription
                    db.pushsubscriptions.Add(new buyselwebapi.model.PushSubscription
                    {
                        email = body.email,
                        endpoint = body.subscription_data.endpoint,
                        p256dh = body.subscription_data.keys?.p256dh,
                        auth = body.subscription_data.keys?.auth,
                        subscriptiontype = "web-push",
                        platform = body.platform ?? "web",
                        createdat = DateTime.UtcNow,
                        lastUsedat = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, message = "Subscription saved" });
            });

            // GET: /api/push_subscription/email/{email} - Get all subscriptions for an email
            group.MapGet("/push_subscription/email/{email}", async (string email, dbcontext db) =>
            {
                var subscriptions = await db.pushsubscriptions
                    .Where(s => s.email == email && s.subscriptiontype == "web-push")
                    .Select(s => new
                    {
                        id = s.id,
                        email = s.email,
                        subscription_data = new
                        {
                            endpoint = s.endpoint,
                            keys = new
                            {
                                p256dh = s.p256dh,
                                auth = s.auth
                            }
                        },
                        created_at = s.createdat,
                        last_used_at = s.lastUsedat,
                        subscription_type = s.subscriptiontype,
                        platform = s.platform
                    })
                    .ToListAsync();

                return Results.Ok(new { subscriptions });
            });

            // DELETE: /api/push_subscription/{id} - Delete expired subscription
            group.MapDelete("/push_subscription/{id}", async (int id, dbcontext db) =>
            {
                var subscription = await db.pushsubscriptions.FindAsync(id);

                if (subscription == null)
                {
                    return Results.NotFound(new { error = "Subscription not found" });
                }

                db.pushsubscriptions.Remove(subscription);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Subscription deleted" });
            });

            // ==========================================
            // NATIVE PUSH ENDPOINTS (for iOS/Android apps)
            // ==========================================

            group.MapPost("/push/unsubscribe-native", async (
                HttpContext context,
                dbcontext db) =>
            {
                var body = await context.Request.ReadFromJsonAsync<NativeUnsubscribeRequest>();

                if (string.IsNullOrEmpty(body?.email))
                {
                    return Results.BadRequest(new { error = "Missing required fields" });
                }

                var subscriptions = await db.pushsubscriptions
                    .Where(s => s.email == body.email && s.subscriptiontype == "native")
                    .ToListAsync();

                db.pushsubscriptions.RemoveRange(subscriptions);
                await db.SaveChangesAsync();

                return Results.Ok(new { success = true, message = "Device unregistered successfully" });
            });

            group.MapPost("/push/subscribe-native", async (
                HttpContext context,
                dbcontext db) =>
            {
                var body = await context.Request.ReadFromJsonAsync<NativeSubscriptionRequest>();

                if (string.IsNullOrEmpty(body?.token) || string.IsNullOrEmpty(body?.email))
                {
                    return Results.BadRequest(new { error = "Missing required fields" });
                }

                var existing = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s =>
                        s.devicetoken == body.token &&
                        s.email == body.email);

                if (existing != null)
                {
                    existing.lastUsedat = DateTime.UtcNow;
                }
                else
                {
                    db.pushsubscriptions.Add(new buyselwebapi.model.PushSubscription
                    {
                        email = body.email,
                        devicetoken = body.token,
                        platform = body.platform,
                        subscriptiontype = "native",
                        createdat = DateTime.UtcNow,
                        lastUsedat = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { success = true, message = "Device registered successfully" });
            });

            // ==========================================
            // LEGACY ENDPOINTS (backwards compatibility)
            // ==========================================

            group.MapPost("/push/subscribe", async (buyselwebapi.model.PushSubscription request, dbcontext db) =>
            {
                var existing = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s => s.endpoint == request.endpoint);

                if (existing != null)
                {
                    existing.email = request.email;
                    existing.p256dh = request.p256dh;
                    existing.auth = request.auth;
                    existing.lastUsedat = DateTime.UtcNow;
                }
                else
                {
                    db.pushsubscriptions.Add(new buyselwebapi.model.PushSubscription
                    {
                        email = request.email,
                        endpoint = request.endpoint,
                        p256dh = request.p256dh,
                        auth = request.auth,
                        createdat = DateTime.UtcNow,
                        lastUsedat = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { success = true });
            });

            group.MapPost("/push/unsubscribe", async (buyselwebapi.model.PushSubscription request, dbcontext db) =>
            {
                var subscription = await db.pushsubscriptions
                    .FirstOrDefaultAsync(s => s.email == request.email && s.endpoint == request.endpoint);

                if (subscription != null)
                {
                    db.pushsubscriptions.Remove(subscription);
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new { success = true });
            });

            group.MapGet("/push/subscriptions/{email}", async (string email, dbcontext db) =>
            {
                var subscriptions = await db.pushsubscriptions
                    .Where(s => s.email == email)
                    .Select(s => new
                    {
                        s.id,
                        s.endpoint,
                        s.createdat,
                        s.lastUsedat
                    })
                    .ToListAsync();

                return Results.Ok(subscriptions);
            });

            // ==========================================
            // HELPER METHODS (keep your existing iOS push methods)
            // ==========================================

            async Task<int> SendIOSPushKeep(buyselwebapi.model.PushSubscription sub, List<string> alerts, IConfiguration config)
            {
                try
                {
                    if (string.IsNullOrEmpty(sub.devicetoken))
                    {
                        Console.WriteLine($"[SendIOSPush] No device token for {sub.email}");
                        return 0;
                    }
                    var p8FileContents = "MIGTAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBHkwdwIBAQQg5OBXHYd8kzhBlg59\r\nNEdRMxZP0YL4oOscO8ufQc6ZcligCgYIKoZIzj0DAQehRANCAAQaxFX54WT09149\r\nQdQ7oW1QFEs3ZbunPYBqCVC1XpuGTCg72Tqqv/Iaptboe6NbmHYp8wpqKjixTUu3\r\neYoIsHqD";

                    var options = new ApnsJwtOptions
                    {
                        BundleId = "com.jobsafepro.app",
                        KeyId = "QXVJA2JD43",
                        TeamId = "B25XZGD4VW",
                        CertContent = p8FileContents
                    };

                    var apns = ApnsClient.CreateUsingJwt(
                        new HttpClient(),
                        options
                    );

                    var push = new ApplePush(ApplePushType.Alert)
                        .AddAlert("Job Safe Pro - Action Required", string.Join(", ", alerts))
                        .AddBadge(alerts.Count)
                        .AddSound("default")
                        .AddToken(sub.devicetoken);

                    Console.WriteLine($"[SendIOSPush] Sending to device token: {sub.devicetoken?.Substring(0, 10)}...");

                    var response = await apns.Send(push);

                    Console.WriteLine($"[SendIOSPush] Response: IsSuccessful={response.IsSuccessful}, Reason={response.Reason}");

                    return response.IsSuccessful ? 1 : 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SendIOSPush] Error: {ex.Message}");
                    return 0;
                }
            }
        }
    }
    public record WebPushSubscriptionRequest(
  string email,
  SubscriptionData subscription_data,
  string? platform
);

    public record SubscriptionData(
        string endpoint,
        SubscriptionKeys? keys
    );

    public record SubscriptionKeys(
        string p256dh,
        string auth
    );

    public record NativeUnsubscribeRequest(string email, string platform);
    public record NativeSubscriptionRequest(string token, string email, string platform);

}