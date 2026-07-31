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
            .AllowCredentials();
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

// --- Removed migrations and seeding block ---
// The schema and admin are already created manually in Supabase.

// Middleware order
app.UseRouting();
app.UseForwardedHeaders();
app.UseCors("AllowFrontend");

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

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogger");
    var origin = context.Request.Headers["Origin"].ToString();
    logger.LogInformation("Incoming {method} {path} from {origin} (scheme={scheme})", context.Request.Method, context.Request.Path, origin, context.Request.Scheme);
    await next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
