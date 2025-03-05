using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortalWebServer.Data;
using PortalWebServer.Service;
using System.Text;
using PortalWebServer.Data;
var builder = WebApplication.CreateBuilder(args);


var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "");
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=db_portalweb;Username=postgres;Password=postgres"));

// Adding Redis service
// It adds and configures Redis distributed cache service 
builder.Services.AddStackExchangeRedisCache(options =>
{
    //This property is set to specify the connection string for Redis
    //The value is fetched from the application's configuration system, i.e., appsettings.json file
    options.Configuration = builder.Configuration["RedisCacheOptions:Configuration"];
    //This property helps in setting a logical name for the Redis cache instance. 
    //The value is also fetched from the appsettings.json file
    options.InstanceName = builder.Configuration["RedisCacheOptions:InstanceName"];
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Tambahkan CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy.AllowAnyOrigin() // Sesuaikan dengan URL Blazor
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<OtpService>();
builder.Services.AddSingleton<JwtService>();

var app = builder.Build();

// Aktifkan CORS sebelum endpoint API dipanggil
app.UseCors("AllowBlazor");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
