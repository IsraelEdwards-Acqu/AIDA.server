using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AIDA.Server.Data;
using AIDA.Server.Services;
using AIDA.Server.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AidaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<KnowledgeService>();
builder.Services.AddHttpClient<TranslationService>();

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // safe without Swagger

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS: explicit origins only
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://aida-bot-ui.vercel.app",    // Vercel UI
                "https://aidabort.netlify.app"       // optional
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // keep if you ever use cookies; otherwise safe to remove
    });
});

// Forwarded headers (Render / proxies)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// --- Apply EF migrations and seed a default admin if missing (safe, idempotent)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AidaDbContext>();

        // Apply pending migrations (creates tables if migrations exist)
        db.Database.Migrate();

        // Seed admin if none exists
        var adminUserName = "admin"; // change if you prefer a different username
        var existing = await db.Admins.FirstOrDefaultAsync(a => a.Username == adminUserName);
        if (existing == null)
        {
            // Read admin password from environment variable (set this in Render)
            var adminPassword = builder.Configuration["ADMIN_PASSWORD"];
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                // If no ADMIN_PASSWORD provided, do not create a default admin automatically in production.
                Console.WriteLine("[Startup] ADMIN_PASSWORD not set; skipping admin seed.");
            }
            else
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                var admin = new Admin
                {
                    Username = adminUserName,
                    PasswordHash = hash,
                    CreatedAt = DateTime.UtcNow
                };
                db.Admins.Add(admin);
                await db.SaveChangesAsync();
                Console.WriteLine("[Startup] Seeded admin user 'admin'.");
            }
        }
        else
        {
            Console.WriteLine("[Startup] Admin user already exists; skipping seed.");
        }
    }
    catch (Exception ex)
    {
        // Log and rethrow so Render logs show the problem
        Console.WriteLine("[Startup] Error applying migrations or seeding admin: " + ex);
        throw;
    }
}

// Middleware order: routing -> forwarded headers -> CORS -> exception handler -> logging -> auth -> endpoints
app.UseRouting();

// Apply forwarded headers first so the request scheme is correct (X-Forwarded-Proto)
app.UseForwardedHeaders();

// Apply CORS early so preflight (OPTIONS) is handled and responses include CORS headers
app.UseCors("AllowFrontend");

// Global exception handler that returns JSON and preserves CORS headers
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
        var exFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exFeature?.Error != null)
        {
            logger.LogError(exFeature.Error, "Unhandled exception processing request {method} {path}", context.Request.Method, context.Request.Path);
        }

        var payload = System.Text.Json.JsonSerializer.Serialize(new { message = "An internal server error occurred." });
        await context.Response.WriteAsync(payload);
    });
});

// Lightweight request logging to help debug 4xx/5xx and CORS issues
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogger");
    var origin = context.Request.Headers["Origin"].ToString();
    logger.LogInformation("Incoming {method} {path} from {origin} (scheme={scheme})", context.Request.Method, context.Request.Path, origin, context.Request.Scheme);
    await next();
});

// On Render the TLS is terminated at the load balancer; UseHttpsRedirection can warn if not configured.
// It's safe to keep UseHttpsRedirection after UseForwardedHeaders so scheme is correct.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
