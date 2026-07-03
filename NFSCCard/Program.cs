using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// load configuration (ensure appsettings.json exists with DefaultConnection and Jwt section)
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for the Vite frontend and local development tools
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// DB connection (transient)
builder.Services.AddTransient<IDbConnection>(sp => new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity password hasher
builder.Services.AddSingleton<IPasswordHasher<NFSCCard.Models.User>>(sp => new PasswordHasher<NFSCCard.Models.User>());

// JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NFSCCardApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NFSCCardClients";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var authorization = ctx.Request.Headers["Authorization"].ToString();
            var token = ctx.Request.Headers["Authorization"].ToString()?.StartsWith("Bearer ") == true
                ? ctx.Request.Headers["Authorization"].ToString().Substring("Bearer ".Length)
                : ctx.Request.Headers["Authorization"].ToString();
            Console.WriteLine($"[JWT] OnMessageReceived Authorization: {authorization}");
            Console.WriteLine($"[JWT] OnMessageReceived Token: {token}");
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            var userId = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = ctx.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            Console.WriteLine($"[JWT] Token validated for UserId={userId}, Role={role}");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine($"[JWT] Authentication failed: {ctx.Exception?.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            Console.WriteLine($"[JWT] Challenge: Error={ctx.Error}, Description={ctx.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// DI: repositories & services
builder.Services.AddScoped<NFSCCard.Repositories.IAuthRepository, NFSCCard.Repositories.AuthRepository>();
builder.Services.AddScoped<NFSCCard.Repositories.ICustomerRepository, NFSCCard.Repositories.CustomerRepository>();
builder.Services.AddScoped<NFSCCard.Services.IAuthService, NFSCCard.Services.AuthService>();
builder.Services.AddScoped<NFSCCard.Services.ICustomerService, NFSCCard.Services.CustomerService>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
