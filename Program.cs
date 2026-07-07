using AIDA.Server.Data;
using AIDA.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database connection
builder.Services.AddDbContext<AidaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// ✅ Add CORS policy to allow Netlify frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNetlify",
        policy => policy.WithOrigins(
            "https://aidabort.netlify.app",        // your Netlify site
            "https://6a4ac4c90c21521564c0dcf7--aidabort.netlify.app" // Netlify preview/deploy URL
        )
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers();

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<KnowledgeService>();
builder.Services.AddHttpClient<TranslationService>();

var app = builder.Build();

// ✅ Enable CORS before auth
app.UseCors("AllowNetlify");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
