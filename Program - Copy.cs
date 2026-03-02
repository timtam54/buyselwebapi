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
        policy.WithOrigins("http://localhost:3000", "https://agreeable-sky-08a3a0e00.2.azurestaticapps.net", "https://buyselapp.icymeadow-c7b88605.australiaeast.azurecontainerapps.io") // Add your production URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Authentication for NextAuth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // This MUST match your NEXTAUTH_SECRET environment variable
        var secret = builder.Configuration["NextAuth:Secret"]
            ?? throw new InvalidOperationException("NextAuth Secret not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),

            // NextAuth doesn't set standard issuer/audience claims by default
            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // Event handlers for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
                Console.WriteLine($"Token validated. Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(token))
                {
                    Console.WriteLine($"Token received: {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddDbContext<dbcontext>(options => options.UseSqlServer("Server=buyselserver.database.windows.net,1433;Initial Catalog=buysel;Persist Security Info=False;User ID=buysel;Password=ABC1234!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
// Enable CORS before authentication
app.UseCors("NextJsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapPropertyEndpoints();
app.MapPropertyPhotoEndpoints();
app.MapUserEndpoints();
app.MapBadgeEndpoints();
app.MapAuditEndpoints();
app.MapConversationEndpoints();
app.MapMessageEndpoints();
app.MapPushSubscrptionEndpoints();
app.MapPropertyBuyerDocEndpoints();
// Public endpoint
/*app.MapGet("/api/public", () => new
{
    message = "This is public data",
    timestamp = DateTime.UtcNow
});

// Protected endpoint - extracts NextAuth user info
app.MapGet("/api/protected", (HttpContext context) =>
{
    var user = context.User;

    // NextAuth JWT claims mapping
    var name = user.FindFirst("name")?.Value;
    var email = user.FindFirst("email")?.Value;
    var picture = user.FindFirst("picture")?.Value;
    var sub = user.FindFirst("sub")?.Value; // User ID

    return Results.Ok(new
    {
        message = "Successfully authenticated!",
        user = new
        {
            id = sub,
            name,
            email,
            picture
        },
        allClaims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
    });
}).RequireAuthorization();

// Example POST endpoint
app.MapPost("/api/data", (HttpContext context, DataRequest request) =>
{
    var email = context.User.FindFirst("email")?.Value;

    return Results.Ok(new
    {
        message = $"Data received from {email}",
        data = request
    });
}).RequireAuthorization();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));*/

app.Run();

// Example request model
record DataRequest(string Title, string Description);