using buyselwebapi.data;
using buyselwebapi.endpoint;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy for your Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("NextJsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://buysel-webapp.azurewebsites.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// #7 - Rate limiting on auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("general", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// #10 - Register IHttpClientFactory for Google Maps geocoding
builder.Services.AddHttpClient("GoogleMaps", client =>
{
    client.BaseAddress = new Uri("https://maps.googleapis.com");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(o =>
{
    // #8 - Enforce HTTPS in production
    o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

    // Use the legacy JwtSecurityTokenHandler for compatibility
    o.UseSecurityTokenValidators = true;

    o.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BuySellCharterTowers123456789012")),
        ValidIssuer = "BuySell",
        ValidAudience = "CharterTowers",
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    o.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"Auth Header: {authHeader?.Substring(0, Math.Min(authHeader?.Length ?? 0, 80))}...");
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                var token = authHeader.Substring(7);
                var parts = token.Split('.');
                Console.WriteLine($"Token parts: {parts.Length}");

                // Explicitly set the token to ensure the handler gets it correctly
                context.Token = token;

                if (parts.Length >= 2)
                {
                    try
                    {
                        var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1].PadRight((parts[1].Length + 3) / 4 * 4, '=')));
                        Console.WriteLine($"Payload: {payload}");
                    }
                    catch (Exception ex) { Console.WriteLine($"Decode error: {ex.Message}"); }
                }
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Auth Failed: {context.Exception.GetType().Name}: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"JWT Token Validated for: {context.Principal?.Identity?.Name}");
            Console.WriteLine($"Claims: {string.Join(", ", context.Principal?.Claims.Select(c => c.Type + "=" + c.Value) ?? Array.Empty<string>())}");
            return Task.CompletedTask;
        }
    };
});

// Configure Database Context
builder.Services.AddDbContext<dbcontext>(options =>
    options.UseSqlServer(
        "Server=buyselserver.database.windows.net,1433;" +
        "Initial Catalog=buysel;" +
        "Persist Security Info=False;" +
        "User ID=buysel;" +
        "Password=ABC1234!;" +
        "MultipleActiveResultSets=False;" +
        "Encrypt=True;" +
        "TrustServerCertificate=False;" +
        "Connection Timeout=30;"
    ));

var app = builder.Build();

// #14 - Only expose Swagger in development
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// #11 - Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

// #8 - Enforce HTTPS
app.UseHttpsRedirection();

// IMPORTANT: Order matters!
// 1. CORS must come before Authentication
app.UseCors("NextJsPolicy");

// #7 - Rate limiter middleware
app.UseRateLimiter();

// 2. Authentication must come before Authorization
app.UseAuthentication();

// 3. Authorization comes last
app.UseAuthorization();

// Map all endpoints - RequireAuthorization applied globally here
// Individual endpoints can use .AllowAnonymous() to opt out (e.g. /cred/ login)
var api = app.MapGroup("").RequireAuthorization().RequireRateLimiting("general");
api.MapPropertyEndpoints();
api.MapPropertyPhotoEndpoints();
api.MapUserEndpoints();
api.MapBadgeEndpoints();
api.MapAuditEndpoints();
api.MapConversationEndpoints();
api.MapMessageEndpoints();
api.MapPushSubscriptionEndpoints();
api.MapPropertyBuyerDocEndpoints();
api.MapUserPropertyFavEndpoints();
api.MapOfferEndpoints();
api.MapOfferConditionEndpoints();
api.MapOfferHistoryEndpoints();


app.Run();
