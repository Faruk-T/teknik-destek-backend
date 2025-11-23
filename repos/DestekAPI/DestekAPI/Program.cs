using DestekAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using DestekAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Connection string’i alıyoruz
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// JWT Ayarlarını al
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

// Sunucuyu tüm ağdan erişilebilir yap
builder.WebHost.UseUrls("http://0.0.0.0:5106");

// CORS ayarları (React ve SignalR için uyumlu)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
    "http://localhost:3000",
    "http://192.168.1.66:3000",
    "http://192.168.1.14:3000",
    "http://192.168.1.20:3000",
    "http://192.168.1.99:3000",
    "http://10.172.110.157:3000",
    "http://192.168.1.195:3000",
    "http://192.168.1.20:5106",
    "http://192.168.1.35:3000",
    "http://192.168.0.10:3000",
    "http://192.168.1.72:3000",
    "http://192.168.1.35:3000",
    "http://localhost:3001",
    "http://192.168.1.66:3001",
    "http://localhost:3002",
    "http://192.168.1.66:3002",
    "http://192.168.1.65:3002",   // <-- Bunu ekle
    "http://localhost:63401",
    "http://192.168.1.66:63401"
)

        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// DbContext’i servis olarak ekliyoruz
builder.Services.AddDbContext<DestekDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DestekAPI", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Lütfen 'Bearer' ile başlayan JWT tokenınızı girin.",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// SignalR servisini ekle
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();
