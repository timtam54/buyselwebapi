using buyselwebapi.data;
using buyselwebapi.endpoint;
using IncidentWebAPI.endpoint;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
              //  "https://agreeable-sky-08a3a0e00.5.azurestaticapps.net",
             //   "https://buyselapp.icymeadow-c7b88605.australiaeast.azurecontainerapps.io",
                "https://buysel-webapp.azurewebsites.net"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure JWT Authentication for Custom JWT System (replaces NextAuth)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Get JWT secret - MUST match the JWT_SECRET in your Next.js .env.local
        var jwtSecret = builder.Configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET is not configured. Please set it in appsettings.json or environment variables.");

        Console.WriteLine($"🔐 JWT Authentication configured. Secret length: {jwtSecret.Length} characters");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validate the signing key
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

            // Validate issuer and audience (these MUST match the Next.js JWT generation)
            ValidateIssuer = true,
            ValidIssuer = "buysel-app", // Must match lib/auth/jwt.ts

            ValidateAudience = true,
            ValidAudience = "buysel-api", // Must match lib/auth/jwt.ts

            // Validate token lifetime
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
        };

        // Event handlers for debugging and monitoring
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ JWT Authentication failed: {context.Exception.Message}");

                if (context.Exception is SecurityTokenExpiredException)
                {
                    Console.WriteLine("⏰ Token has expired");
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                {
                    Console.WriteLine("🔑 Invalid token signature - JWT_SECRET may not match");
                }
                else if (context.Exception is SecurityTokenInvalidIssuerException)
                {
                    Console.WriteLine("🏢 Invalid issuer - expected 'buysel-app'");
                }
                else if (context.Exception is SecurityTokenInvalidAudienceException)
                {
                    Console.WriteLine("👥 Invalid audience - expected 'buysel-api'");
                }

                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                var userId = context.Principal?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var userEmail = context.Principal?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                var userName = context.Principal?.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

                Console.WriteLine($"✅ JWT Token validated - User: {userName} ({userEmail}), ID: {userId}");

                return Task.CompletedTask;
            },

            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                if (!string.IsNullOrEmpty(authHeader))
                {
                    var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring(7) : authHeader;
                    Console.WriteLine($"📨 JWT Token received: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                else
                {
                    var path = context.Request.Path;
                    // Only log missing tokens for protected endpoints
                    if (!path.StartsWithSegments("/api/users/oauth") &&
                        !path.StartsWithSegments("/health") &&
                        !path.StartsWithSegments("/swagger"))
                    {
                        Console.WriteLine($"⚠️  No Authorization header found for: {path}");
                    }
                }

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                Console.WriteLine($"🚫 Authentication challenge: {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production - still show Swagger but maybe on a different path
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT: Order matters!
// 1. CORS must come before Authentication
app.UseCors("NextJsPolicy");

// 2. Authentication must come before Authorization
app.UseAuthentication();

// 3. Authorization comes last
app.UseAuthorization();

// Map all endpoints
app.MapPropertyEndpoints();
app.MapPropertyPhotoEndpoints();
app.MapUserEndpoints();
app.MapBadgeEndpoints();
app.MapAuditEndpoints();
app.MapConversationEndpoints();
app.MapMessageEndpoints();
app.MapPushSubscrptionEndpoints();
app.MapPropertyBuyerDocEndpoints();
app.MapUserPropertyFavEndpoints();
app.MapOfferEndpoints();
app.MapOfferConditionEndpoints();
app.MapOfferHistoryEndpoints();
// Health check endpoint (public, no auth required)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    authentication = "Custom JWT with OAuth 2.0"
}))
.WithName("HealthCheck")
.WithTags("Health")
.WithOpenApi();

Console.WriteLine("🚀 BuySel API Server starting...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔐 Authentication: Custom JWT (issuer: buysel-app, audience: buysel-api)");
Console.WriteLine($"🌐 CORS Enabled for Next.js frontend");

app.Run();
