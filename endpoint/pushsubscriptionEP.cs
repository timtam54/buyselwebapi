using buyselwebapi.data;
using dotAPNS;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace buyselwebapi.endpoint
{
    /// <summary>
    /// Push notification subscription endpoints: web push (VAPID) and native iOS/Android.
    /// Manages subscription lifecycle: subscribe, unsubscribe, list by email.
    /// Web push uses endpoint+p256dh+auth keys. Native uses device tokens.
    /// Subscriptions are deduplicated by endpoint (web) or device token (native).
    /// </summary>
    public static class pushsubscriptionEP
    {
        public static void MapPushSubscriptionEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/push").WithTags(nameof(pushsubscriptionEP));

            // Unsubscribe native device
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

            // Subscribe native device
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

            // Subscribe to web push
            group.MapPost("/subscribe", async (buyselwebapi.model.PushSubscription request, dbcontext db) =>
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

            // Unsubscribe from web push
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

            // Get subscriptions by email
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

            // Create/Update web push subscription (with subscription_data format)
            group.MapPost("/push_subscription", async (HttpContext context, dbcontext db, ILogger<dbcontext> logger) =>
            {
                try
                {
                    var body = await context.Request.ReadFromJsonAsync<WebPushSubscriptionRequest>();

                    if (string.IsNullOrEmpty(body?.email) || body?.subscription_data == null)
                    {
                        return Results.BadRequest(new { error = "Invalid subscription data" });
                    }

                    var existing = await db.pushsubscriptions
                        .FirstOrDefaultAsync(s => s.endpoint == body.subscription_data.endpoint);

                    if (existing != null)
                    {
                        existing.email = body.email;
                        existing.p256dh = body.subscription_data.keys?.p256dh;
                        existing.auth = body.subscription_data.keys?.auth;
                        existing.lastUsedat = DateTime.UtcNow;
                        existing.subscriptiontype = "web-push";
                        existing.platform = body.platform ?? "web";
                    }
                    else
                    {
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
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to save push subscription");
                    return Results.Problem(
                        detail: "An unexpected error occurred",
                        statusCode: 500
                    );
                }
            });

            // Get web-push subscriptions with full details
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

            // Delete subscription by ID
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
        }

        /// <summary>
        /// Sends an iOS push notification via APNS using dotAPNS library.
        /// Returns 1 on success, 0 on failure. Uses P8 key authentication.
        /// </summary>
        public static async Task<int> SendIOSPush(buyselwebapi.model.PushSubscription sub, List<string> alerts, IConfiguration config, ILogger logger)
        {
            try
            {
                if (string.IsNullOrEmpty(sub.devicetoken))
                {
                    logger.LogWarning("No device token for {Email}", sub.email);
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

                logger.LogInformation("Sending iOS push to device {DeviceToken}", sub.devicetoken?.Substring(0, Math.Min(10, sub.devicetoken?.Length ?? 0)));

                var response = await apns.Send(push);

                logger.LogInformation("iOS push response: IsSuccessful={IsSuccessful}, Reason={Reason}", response.IsSuccessful, response.Reason);

                return response.IsSuccessful ? 1 : 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send iOS push notification");
                return 0;
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
