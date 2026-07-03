# NFSCCard Project Architecture Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture Layers](#architecture-layers)
3. [Project Structure](#project-structure)
4. [Technology Stack](#technology-stack)
5. [Core Components](#core-components)
6. [Data Flow](#data-flow)
7. [Configuration & Dependency Injection](#configuration--dependency-injection)
8. [Security & Authentication](#security--authentication)
9. [Design Patterns](#design-patterns)
10. [API Endpoints](#api-endpoints)

---

## Project Overview

**Project Name:** NFSCCard  
**Framework:** .NET 8 ASP.NET Core  
**Architecture Pattern:** 3-Tier N-Layer Architecture  
**Primary Purpose:** REST API for managing NFSC Card (National Financial Services Company Card) customers with JWT-based authentication

The application follows industry best practices with clear separation of concerns, dependency injection, and async/await patterns throughout.

---

## Architecture Layers

### Layer Structure

```
???????????????????????????????????????????
?     PRESENTATION LAYER (Controllers)    ?
?  - HTTP Request/Response Handling       ?
?  - Authorization & Authentication       ?
?  - Input Validation                     ?
???????????????????????????????????????????
                     ?
???????????????????????????????????????????
?   BUSINESS LOGIC LAYER (Services)       ?
?  - Business Rules Implementation        ?
?  - JWT Token Generation                 ?
?  - Password Hashing & Verification      ?
?  - Orchestration & Coordination         ?
???????????????????????????????????????????
                     ?
???????????????????????????????????????????
?   DATA ACCESS LAYER (Repositories)      ?
?  - Database Operations via Dapper       ?
?  - Stored Procedure Execution           ?
?  - Data Mapping & Transformation        ?
???????????????????????????????????????????
                     ?
???????????????????????????????????????????
?         DATABASE LAYER                  ?
?  - SQL Server                           ?
?  - Stored Procedures                    ?
?  - Tables & Relationships                ?
???????????????????????????????????????????
```

### Layer Responsibilities

#### **Presentation Layer (Controllers)**
- Receives HTTP requests
- Validates input and applies authorization attributes
- Delegates business logic to services
- Returns HTTP responses (OK, NotFound, Unauthorized, etc.)
- Extracts claims from JWT tokens for user context

#### **Business Logic Layer (Services)**
- Implements core business rules
- Generates and validates JWT tokens
- Handles password hashing and verification
- Orchestrates multiple repositories
- Provides higher-level operations

#### **Data Access Layer (Repositories)**
- Encapsulates all database operations
- Uses Dapper ORM for executing stored procedures
- Maps database results to DTOs
- Manages connection strings and parameters
- Provides clean abstraction over SQL

#### **Data Models & DTOs**
- **Models:** Domain entities (User, Customer)
- **DTOs:** Data transfer objects for API contracts
- Separation prevents exposing internal entities to clients

---

## Project Structure

```
NFSCCard/
?
??? Program.cs                           # Application startup & configuration
??? NFSCCard.csproj                      # Project file with NuGet dependencies
??? ARCHITECTURE.md                      # This file
?
??? Models/                              # Domain entities
?   ??? User.cs                          # User entity with authentication properties
?
??? DTOs/                                # Data Transfer Objects for API contracts
?   ??? Auth/
?   ?   ??? RegisterDto.cs               # Registration request DTO
?   ?   ??? LoginDto.cs                  # Login request DTO
?   ?   ??? AuthResponseDto.cs           # Authentication response DTO
?   ??? Customer/
?       ??? CustomerDto.cs               # Customer data DTO
?
??? Repositories/                        # Data Access Layer (DAL)
?   ??? ICustomerRepository.cs           # Customer repository interface
?   ??? CustomerRepository.cs            # Customer repository implementation
?   ??? IAuthRepository.cs               # Auth repository interface
?   ??? AuthRepository.cs                # Auth repository implementation
?
??? Services/                            # Business Logic Layer (BLL)
?   ??? IAuthService.cs                  # Auth service interface
?   ??? AuthService.cs                   # Auth service implementation
?   ??? ICustomerService.cs              # Customer service interface
?   ??? CustomerService.cs               # Customer service implementation
?
??? Controllers/                         # Presentation Layer (API Endpoints)
?   ??? AuthController.cs                # Authentication endpoints
?   ??? CustomersController.cs           # Customer management endpoints
?   ??? NfscController.cs                # NFSC-specific endpoints
?
??? Properties/
?   ??? launchSettings.json              # Debug profile settings
?
??? appsettings.json                     # Configuration file (connection strings, JWT)
```

---

## Technology Stack

### Core Framework
- **Framework:** .NET 8.0
- **Web API:** ASP.NET Core
- **Language:** C# 12

### Data Access
- **ORM:** Dapper 2.1.15
  - Lightweight, high-performance ORM
  - Excellent for stored procedure execution
  - Minimal overhead vs Entity Framework
- **Database:** SQL Server
- **Connection:** System.Data.SqlClient 4.8.6

### Authentication & Security
- **JWT:** System.IdentityModel.Tokens.Jwt
  - Token-based stateless authentication
  - Claims-based authorization
- **Password Hashing:** Microsoft.AspNetCore.Identity (PasswordHasher)
  - Implements bcrypt algorithm
  - One-way hashing for security
- **Authentication Scheme:** Microsoft.AspNetCore.Authentication.JwtBearer 8.0.8

### API Documentation
- **Swagger:** Swashbuckle.AspNetCore 10.2.3
  - Interactive API documentation
  - Test endpoints directly from UI
  - OpenAPI specification generation

### Dependencies Summary
```xml
<ItemGroup>
  <PackageReference Include="Dapper" Version="2.1.15" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication" Version="2.3.11" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
  <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.19.1" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.3" />
  <PackageReference Include="System.Data.SqlClient" Version="4.8.6" />
</ItemGroup>
```

---

## Core Components

### Models

#### **User.cs**
```csharp
public class User
{
    public int UserId { get; set; }              // Primary key
    public string? Email { get; set; }           // User email
    public string? PasswordHash { get; set; }    // Bcrypt password hash
    public string? Role { get; set; }            // User role (Admin, Customer, etc.)
    public bool IsActive { get; set; } = true;   // Soft delete flag
}
```
- **Purpose:** Domain entity representing a user in the system
- **PasswordHash:** Never stores plain passwords; uses bcrypt one-way hashing
- **IsActive:** Enables soft deletes without removing records

### Data Transfer Objects (DTOs)

#### **RegisterDto.cs** - Registration Request
```csharp
public class RegisterDto
{
    public string Email { get; set; } = null!;              // Required
    public string Password { get; set; } = null!;          // Plain text (will be hashed)
    public string FirstName { get; set; } = null!;         // Required
    public string? LastName { get; set; }                  // Optional
}
```
- **Usage:** POST /api/auth/register
- **Security:** Password never stored as plain text

#### **LoginDto.cs** - Login Request
```csharp
public class LoginDto
{
    public string Email { get; set; } = null!;    // Required
    public string Password { get; set; } = null!; // Plain text
}
```
- **Usage:** POST /api/auth/login
- **Authentication:** Credentials verified against bcrypt hash

#### **AuthResponseDto.cs** - Authentication Response
```csharp
public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;    // JWT token
    public DateTime ExpiresAt { get; set; };             // Token expiration time
    public string? RefreshToken { get; set; };           // 7-day refresh token
    public int UserId { get; set; };                     // User ID from token
    public string Role { get; set; } = null!;            // User role
}
```
- **AccessToken:** Short-lived JWT (default 60 minutes)
- **RefreshToken:** Long-lived token (7 days) to get new access tokens

#### **CustomerDto.cs** - Customer Data
```csharp
public class CustomerDto
{
    public int CustomerId { get; set; };          // Primary key
    public int UserId { get; set; };              // User association
    public string? NFCCodeUniqueId { get; set; }; // NFSC card code
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; };
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; };
    public bool IsActive { get; set; };
    public DateTime CreatedDate { get; set; };
}
```
- **NFCCodeUniqueId:** Unique identifier for NFSC card
- **UserId:** Links customer to user account

### Repositories (Data Access Layer)

#### **ICustomerRepository Interface**
```csharp
public interface ICustomerRepository
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int customerId);
    Task<CustomerDto?> GetByNfscCodeAsync(string nfscCode);
    Task SaveAsync(DynamicParameters parameters);
    Task DeleteAsync(int customerId);
}
```

#### **CustomerRepository Implementation**
```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnection _db;

    // Constructor injection of database connection
    public CustomerRepository(IDbConnection db) => _db = db;

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        // Execute stored procedure that returns collection
        return await _db.QueryAsync<CustomerDto>(
            "SP_Customer_List",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<CustomerDto?> GetByIdAsync(int customerId)
    {
        // Create parameter container for stored procedure
        var p = new DynamicParameters();
        p.Add("@CustomerId", customerId);

        // Execute stored procedure, map result to DTO
        return await _db.QueryFirstOrDefaultAsync<CustomerDto>(
            "SP_Customer_GetById",
            p,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task SaveAsync(DynamicParameters parameters)
    {
        // Execute non-query stored procedure (INSERT/UPDATE)
        await _db.ExecuteAsync(
            "SP_Customer_Save",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task DeleteAsync(int customerId)
    {
        var p = new DynamicParameters();
        p.Add("@CustomerId", customerId);

        // Execute delete stored procedure
        await _db.ExecuteAsync(
            "SP_Customer_Delete",
            p,
            commandType: CommandType.StoredProcedure
        );
    }
}
```

**Dapper Methods Used:**
- `QueryAsync<T>()`: Execute SELECT, return collection of mapped objects
- `QueryFirstOrDefaultAsync<T>()`: Execute SELECT, return first result or null
- `ExecuteAsync()`: Execute non-query command (INSERT, UPDATE, DELETE)

#### **IAuthRepository & AuthRepository**
```csharp
public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<int> CreateUserAndCustomerAsync(DynamicParameters parameters);
}

public class AuthRepository : IAuthRepository
{
    private readonly IDbConnection _db;
    public AuthRepository(IDbConnection db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email)
    {
        var p = new DynamicParameters();
        p.Add("@Email", email);

        // Fetch user by email for authentication
        return await _db.QueryFirstOrDefaultAsync<User>(
            "SP_LoginUser",
            p,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<int> CreateUserAndCustomerAsync(DynamicParameters parameters)
    {
        // Create user + customer in database
        await _db.ExecuteAsync(
            "SP_Customer_Save",
            parameters,
            commandType: CommandType.StoredProcedure
        );
        return 1;
    }
}
```

### Services (Business Logic Layer)

#### **IAuthService Interface**
```csharp
public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
}
```

#### **AuthService Implementation**

```csharp
public class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly IConfiguration _config;
    private readonly IPasswordHasher<User> _passwordHasher;

    // In-memory refresh token store (consider moving to database in production)
    private static readonly ConcurrentDictionary<string, (int userId, DateTime expires)> _refreshStore = new();

    public AuthService(
        IAuthRepository repo,
        IConfiguration config,
        IPasswordHasher<User> passwordHasher)
    {
        _repo = repo;
        _config = config;
        _passwordHasher = passwordHasher;
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        // Create temporary user object for hashing
        var tempUser = new User { Email = dto.Email };
        var hash = _passwordHasher.HashPassword(tempUser, dto.Password);

        // Build stored procedure parameters
        var p = new DynamicParameters();
        p.Add("@CustomerId", 0);
        p.Add("@UserId", dbType: System.Data.DbType.Int32, value: null);
        p.Add("@FirstName", dto.FirstName);
        p.Add("@LastName", dto.LastName);
        p.Add("@Email", dto.Email);
        p.Add("@PhoneNumber", null);
        // ... additional fields ...
        p.Add("@PasswordHash", hash);

        // Save user + customer to database
        await _repo.CreateUserAndCustomerAsync(p);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Fetch user by email
        var user = await _repo.GetByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials");

        // Verify password against stored hash
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash ?? "",
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials");

        // Generate JWT + refresh token
        var (token, expires) = GenerateJwtToken(user);
        var refresh = GenerateRefreshToken(user.UserId);

        return new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAt = expires,
            RefreshToken = refresh,
            UserId = user.UserId,
            Role = user.Role ?? "Customer"
        };
    }

    private (string token, DateTime expires) GenerateJwtToken(User user)
    {
        // Get configuration values
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        // Create claims (user identity data embedded in token)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, user.Role ?? "Customer")
        };

        // Build JWT token
        var jwt = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        // Convert token to string
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expires);
    }

    private string GenerateRefreshToken(int userId)
    {
        // Create random Base64-encoded refresh token
        var rt = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expires = DateTime.UtcNow.AddDays(7);

        // Store in memory (should use database in production)
        _refreshStore[rt] = (userId, expires);
        return rt;
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return null;

        // Check if refresh token exists and is valid
        if (!_refreshStore.TryGetValue(refreshToken, out var info)) return null;
        if (info.expires < DateTime.UtcNow)
        {
            _refreshStore.TryRemove(refreshToken, out _);
            return null;
        }

        // Generate new access token
        var user = new User { UserId = info.userId, Role = "Customer" };
        var (token, expires) = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            AccessToken = token,
            ExpiresAt = expires,
            RefreshToken = refreshToken,
            UserId = info.userId,
            Role = "Customer"
        };
    }
}
```

#### **ICustomerService & CustomerService**

```csharp
public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int customerId);
    Task<CustomerDto?> GetByNfscAsync(string nfscCode);
    Task SaveAsync(DynamicParameters parameters);
    Task DeleteAsync(int customerId);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;

    public CustomerService(ICustomerRepository repo) => _repo = repo;

    // Pass-through pattern - delegates to repository
    public async Task<IEnumerable<CustomerDto>> GetAllAsync() 
        => await _repo.GetAllAsync();

    public async Task<CustomerDto?> GetByIdAsync(int customerId) 
        => await _repo.GetByIdAsync(customerId);

    public async Task<CustomerDto?> GetByNfscAsync(string nfscCode) 
        => await _repo.GetByNfscCodeAsync(nfscCode);

    public async Task SaveAsync(DynamicParameters parameters) 
        => await _repo.SaveAsync(parameters);

    public async Task DeleteAsync(int customerId) 
        => await _repo.DeleteAsync(customerId);
}
```

### Controllers (Presentation Layer)

#### **AuthController**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>
    /// Register a new user and create associated customer record
    /// </summary>
    /// <param name="dto">Registration details</param>
    /// <returns>Success message</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        await _auth.RegisterAsync(dto);
        return Ok(new { message = "Registered successfully" });
    }

    /// <summary>
    /// Login user and receive JWT + refresh token
    /// </summary>
    /// <param name="dto">Email and password</param>
    /// <returns>Authentication tokens and user info</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var res = await _auth.LoginAsync(dto);
            return Ok(res);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="refreshToken">Valid refresh token</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var res = await _auth.RefreshTokenAsync(refreshToken);
        if (res == null) return Unauthorized();
        return Ok(res);
    }
}
```

#### **CustomersController**

```csharp
[ApiController]
[Authorize]  // All endpoints require valid JWT
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _svc;
    public CustomersController(ICustomerService svc) => _svc = svc;

    /// <summary>
    /// Get all customers (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _svc.GetAllAsync();
        return Ok(list);
    }

    /// <summary>
    /// Get customer by ID (users can only see their own, admins see all)
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cust = await _svc.GetByIdAsync(id);
        if (cust == null) return NotFound();

        // Authorization check: non-admins can only view their own record
        if (!User.IsInRole("Admin"))
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (uid != cust.UserId) return Forbid();
        }

        return Ok(cust);
    }

    /// <summary>
    /// Create new customer (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto)
    {
        var p = new DynamicParameters();
        p.Add("@CustomerId", 0);
        p.Add("@FirstName", dto.FirstName);
        p.Add("@LastName", dto.LastName);
        p.Add("@Email", dto.Email);
        p.Add("@PhoneNumber", dto.PhoneNumber);
        // ... additional fields ...

        await _svc.SaveAsync(p);
        return CreatedAtAction(nameof(GetById), new { id = dto.CustomerId }, null);
    }

    /// <summary>
    /// Update customer (users can update their own, admins can update any)
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
    {
        var existing = await _svc.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Authorization check
        if (!User.IsInRole("Admin"))
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (uid != existing.UserId) return Forbid();
        }

        var p = new DynamicParameters();
        p.Add("@CustomerId", id);
        p.Add("@UserId", existing.UserId);
        p.Add("@FirstName", dto.FirstName);
        // ... additional fields ...

        await _svc.SaveAsync(p);
        return NoContent();
    }

    /// <summary>
    /// Delete customer (Admin only)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _svc.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _svc.DeleteAsync(id);
        return NoContent();
    }
}
```

---

## Data Flow

### Login Request Flow

```
1. CLIENT REQUEST
   POST /api/auth/login
   {
     "email": "user@test.com",
     "password": "MyPassword123"
   }

2. PRESENTATION LAYER (AuthController)
   ?? Receives LoginDto
   ?? Calls AuthService.LoginAsync()

3. BUSINESS LOGIC LAYER (AuthService)
   ?? Calls AuthRepository.GetByEmailAsync(email)
   ?  ?? Executes: SELECT * FROM Users WHERE Email = @Email
   ?? Validates user exists and IsActive = true
   ?? Verifies password hash using PasswordHasher
   ?  ?? Compares stored bcrypt hash with provided password
   ?? Generates JWT token with claims
   ?  ?? UserId
   ?  ?? Email
   ?  ?? Role
   ?  ?? Expiry (60 minutes from now)
   ?? Signs token with secret key (HS256)
   ?? Generates refresh token (7-day expiry)
   ?? Returns AuthResponseDto

4. PRESENTATION LAYER (AuthController)
   ?? Returns 200 OK with:
      {
        "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "expiresAt": "2025-01-15T10:30:00Z",
        "refreshToken": "YWJjMTIzLXl4eg==",
        "userId": 1,
        "role": "Customer"
      }

5. CLIENT STORAGE
   ?? Stores accessToken in localStorage/sessionStorage
      Stores refreshToken securely (httpOnly cookie preferred)
```

### Authenticated Request Flow

```
1. CLIENT REQUEST
   GET /api/customers/1
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

2. MIDDLEWARE (Authentication)
   ?? Extracts token from Authorization header
   ?? Validates token signature using secret key
   ?? Checks token expiry
   ?? Extracts claims (UserId, Email, Role)
   ?? Populates HttpContext.User with claims

3. PRESENTATION LAYER (CustomersController)
   ?? [Authorize] attribute checks if user authenticated
   ?  ?? Validates HttpContext.User is set
   ?? Calls GetById(id)
   ?? Extracts UserId from claims: User.FindFirstValue(ClaimTypes.NameIdentifier)
   ?? Calls CustomerService.GetByIdAsync(id)
   ?? Validates authorization (user can only see own record)

4. BUSINESS LOGIC LAYER (CustomerService)
   ?? Delegates to CustomerRepository.GetByIdAsync(id)

5. DATA ACCESS LAYER (CustomerRepository)
   ?? Creates DynamicParameters
   ?? Adds @CustomerId parameter
   ?? Executes: EXEC SP_Customer_GetById @CustomerId = 1

6. DATABASE
   ?? Returns customer record

7. RETURN PATH
   ?? CustomerRepository maps SQL result to CustomerDto
   ?? CustomerService returns DTO
   ?? CustomersController returns 200 OK
   ?? Client receives customer data
```

---

## Configuration & Dependency Injection

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// ============================================
// 1. CORE SERVICES
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// 2. CORS CONFIGURATION
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

// ============================================
// 3. DATABASE CONNECTION
// ============================================
// Transient: New instance per HTTP request
// Ensures connection isolation and cleanup
builder.Services.AddTransient<IDbConnection>(sp =>
    new SqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ============================================
// 4. PASSWORD HASHING
// ============================================
// Singleton: One instance for app lifetime
// Uses bcrypt algorithm via ASP.NET Identity
builder.Services.AddSingleton<IPasswordHasher<NFSCCard.Models.User>>(sp =>
    new PasswordHasher<NFSCCard.Models.User>()
);

// ============================================
// 5. JWT AUTHENTICATION
// ============================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key missing from configuration");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NFSCCardApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NFSCCardClients";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    // Set default schemes for challenge & authentication
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate token issuer matches config value
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        // Validate intended audience
        ValidateAudience = true,
        ValidAudience = jwtAudience,

        // Verify token was signed with correct secret key
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

        // Reject expired tokens
        ValidateLifetime = true,

        // Allow 30 seconds clock skew for time differences
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// ============================================
// 6. DEPENDENCY INJECTION - REPOSITORIES & SERVICES
// ============================================
// Scoped: New instance per HTTP request
// Enables per-request state management

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddAuthorization();

// ============================================
// 7. BUILD & CONFIGURE MIDDLEWARE
// ============================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// MIDDLEWARE PIPELINE (order matters!)
app.UseHttpsRedirection();      // Redirect HTTP to HTTPS
app.UseCors("DefaultCors");     // Enable CORS
app.UseAuthentication();        // Check JWT token
app.UseAuthorization();         // Check roles/policies
app.MapControllers();           // Route requests to controllers

app.Run();
```

### appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NFSCCardDB;Integrated Security=true;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "NFSCCardApi",
    "Audience": "NFSCCardClients",
    "ExpiresMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Dependency Injection Lifetimes

| Lifetime | Usage | When to Use |
|----------|-------|-------------|
| **Transient** | New instance every time | `IDbConnection` - ensure cleanup per request |
| **Scoped** | New instance per HTTP request | Repositories, Services - per-request state |
| **Singleton** | Single instance for app lifetime | `PasswordHasher` - stateless utility, `Configuration` |

---

## Security & Authentication

### JWT Token Structure

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c

Header: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
{
  "alg": "HS256",
  "typ": "JWT"
}

Payload: eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ
{
  "sub": "1234567890",
  "iat": 1516239022,
  "exp": 1516242622,
  "iss": "NFSCCardApi",
  "aud": "NFSCCardClients",
  "email": "user@test.com",
  "role": "Customer",
  "nameidentifier": "1"
}

Signature: SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
(HMACSHA256(header.payload, secret_key))
```

### Password Security

```csharp
// Registration - Hash Password
var tempUser = new User { Email = dto.Email };
var hash = _passwordHasher.HashPassword(tempUser, dto.Password);
// Result: $2a$11$8hSi8YZ8X5...LbP2DRy (bcrypt hash)
// Never store plain password

// Login - Verify Password
var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
if (result == PasswordVerificationResult.Success)
{
    // Password matches - login successful
}
else if (result == PasswordVerificationResult.Failed)
{
    // Password does not match
}
```

### Authorization Attributes

```csharp
// No authentication required
public async Task<IActionResult> PublicEndpoint() { }

// Requires valid JWT token
[Authorize]
public async Task<IActionResult> ProtectedEndpoint() { }

// Requires Admin role
[Authorize(Roles = "Admin")]
public async Task<IActionResult> AdminOnly() { }

// Multiple roles
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> MultiRoleEndpoint() { }

// Multiple policies
[Authorize(Policy = "AtLeast21")]
public async Task<IActionResult> PolicyBasedEndpoint() { }
```

### Claims-Based Authorization

```csharp
// Extract claims from authenticated user
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);     // "1"
var email = User.FindFirstValue(ClaimTypes.Email);               // "user@test.com"
var role = User.FindFirstValue(ClaimTypes.Role);                 // "Customer"

// Check if user has specific role
if (User.IsInRole("Admin")) { }

// Custom authorization logic
if (!User.IsInRole("Admin"))
{
    var uid = int.Parse(userId ?? "0");
    if (uid != customer.UserId) return Forbid();  // User can't access other's data
}
```

---

## Design Patterns

### 1. **Repository Pattern**
Abstracts database operations behind interface, enabling:
- Loose coupling between layers
- Easy testing with mock repositories
- Centralized data access logic

```csharp
// Interface defines contract
public interface ICustomerRepository
{
    Task<CustomerDto?> GetByIdAsync(int customerId);
}

// Implementation encapsulates database details
public class CustomerRepository : ICustomerRepository
{
    public async Task<CustomerDto?> GetByIdAsync(int customerId)
    {
        // Database-specific code here
    }
}
```

### 2. **Dependency Injection**
- Constructor injection passes dependencies to classes
- DI container (from Program.cs) manages object creation
- Enables loose coupling and testability

```csharp
public class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher<User> _passwordHasher;

    // Dependencies injected via constructor
    public AuthService(IAuthRepository repo, IPasswordHasher<User> passwordHasher)
    {
        _repo = repo;
        _passwordHasher = passwordHasher;
    }
}

// DI container instantiates and injects
builder.Services.AddScoped<IAuthService, AuthService>();
```

### 3. **Service Layer Pattern**
- Encapsulates business logic
- Coordinates multiple repositories
- Provides high-level operations to controllers

```csharp
public class AuthService : IAuthService
{
    // Coordinates authentication:
    // - Repository for data access
    // - Password hasher for security
    // - JWT generation for tokens
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // Complex business logic here
    }
}
```

### 4. **DTO (Data Transfer Object) Pattern**
- Separates API contracts from internal entities
- Prevents exposing database structure
- Enables input validation

```csharp
// API request DTO
public class RegisterDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

// Internal User entity (different structure)
public class User
{
    public int UserId { get; set; }
    public string? PasswordHash { get; set; }  // Never exposed in DTO
}
```

### 5. **Async/Await Pattern**
- Non-blocking I/O operations
- Better resource utilization
- Improved scalability

```csharp
// Async all the way down
public async Task<CustomerDto?> GetByIdAsync(int customerId)
{
    return await _db.QueryFirstOrDefaultAsync<CustomerDto>(
        "SP_Customer_GetById",
        p,
        commandType: CommandType.StoredProcedure
    );
}
```

### 6. **Factory Pattern (via DI)**
```csharp
// DI container acts as factory
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Container creates instances:
var authRepo = new AuthRepository(dbConnection); // Dependencies injected
```

---

## API Endpoints

### Authentication Endpoints

#### Register New User
```
POST /api/auth/register
Content-Type: application/json

{
  "email": "newuser@test.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}

Response 200:
{
  "message": "Registered successfully"
}
```

#### Login
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "SecurePassword123!"
}

Response 200:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-01-15T10:30:00Z",
  "refreshToken": "YWJjMTIzLXl4eg==",
  "userId": 1,
  "role": "Customer"
}

Response 401:
{
  "message": "Invalid credentials"
}
```

#### Refresh Token
```
POST /api/auth/refresh-token
Content-Type: application/json

"YWJjMTIzLXl4eg=="

Response 200:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-01-15T10:30:00Z",
  "refreshToken": "YWJjMTIzLXl4eg==",
  "userId": 1,
  "role": "Customer"
}

Response 401:
(Unauthorized)
```

### Customer Management Endpoints

#### Get All Customers (Admin Only)
```
GET /api/customers
Authorization: Bearer {accessToken}

Response 200:
[
  {
    "customerId": 1,
    "userId": 1,
    "nfcCodeUniqueId": "NFSC-12345",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@test.com",
    "phoneNumber": "+1-555-1234",
    "isActive": true,
    "createdDate": "2025-01-10T12:00:00Z"
  }
]

Response 401: Unauthorized
Response 403: Forbidden (Not Admin)
```

#### Get Customer by ID
```
GET /api/customers/1
Authorization: Bearer {accessToken}

Response 200:
{
  "customerId": 1,
  "userId": 1,
  "nfcCodeUniqueId": "NFSC-12345",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@test.com",
  "phoneNumber": "+1-555-1234",
  "isActive": true,
  "createdDate": "2025-01-10T12:00:00Z"
}

Response 404: Not Found
Response 401: Unauthorized
Response 403: Forbidden (Not owner or admin)
```

#### Create Customer (Admin Only)
```
POST /api/customers
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "customerId": 0,
  "userId": 1,
  "nfcCodeUniqueId": "NFSC-12345",
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@test.com",
  "phoneNumber": "+1-555-5678",
  "isActive": true,
  "createdDate": "2025-01-10T12:00:00Z"
}

Response 201: Created
Location: /api/customers/{id}
```

#### Update Customer
```
PUT /api/customers/1
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "customerId": 1,
  "userId": 1,
  "firstName": "John",
  "lastName": "Smith",
  "email": "john.smith@test.com",
  "phoneNumber": "+1-555-9999",
  "isActive": true,
  "createdDate": "2025-01-10T12:00:00Z"
}

Response 204: No Content
Response 404: Not Found
Response 401: Unauthorized
Response 403: Forbidden
```

#### Delete Customer (Admin Only)
```
DELETE /api/customers/1
Authorization: Bearer {accessToken}

Response 204: No Content
Response 404: Not Found
Response 401: Unauthorized
Response 403: Forbidden
```

---

## Summary

### Architecture Highlights
? **Clean Separation of Concerns** - Clear boundaries between layers  
? **Dependency Injection** - Loose coupling, testable code  
? **Async/Await** - Non-blocking operations, better scalability  
? **JWT Security** - Stateless authentication with token-based access  
? **Password Security** - Bcrypt hashing, never storing plain passwords  
? **Repository Pattern** - Data access abstraction enabling flexibility  
? **DTO Pattern** - Separates API contracts from internal entities  
? **Role-Based Authorization** - Admin vs Customer access control  

### Technology Choices
- **Dapper ORM** - Lightweight, high-performance for stored procedures
- **SQL Server** - Robust relational database
- **.NET 8** - Latest .NET framework with performance improvements
- **ASP.NET Core** - Modern web framework with built-in DI and security

### Improvement Opportunities (Future Enhancements)
- Move refresh tokens to Redis cache or database (currently in-memory)
- Add refresh token rotation for enhanced security
- Implement logging and monitoring
- Add request/response logging middleware
- Implement rate limiting
- Add comprehensive input validation
- Add unit tests and integration tests
- Implement API versioning
- Add comprehensive error handling and custom exception handlers
- Document stored procedures in database

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**Framework:** .NET 8  
**Architecture Pattern:** 3-Tier N-Layer with Repository & Service Patterns
