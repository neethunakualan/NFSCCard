using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Dapper;
using NFSCCard.DTOs.Auth;
using NFSCCard.Models;
using NFSCCard.Repositories;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NFSCCard.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        // Simple in-memory refresh store (replace with DB)
        private static readonly ConcurrentDictionary<string, (int userId, DateTime expires)> _refreshStore = new();

        public AuthService(IAuthRepository repo, IConfiguration config, IPasswordHasher<User> passwordHasher, ILogger<AuthService> logger)
        {
            _repo = repo;
            _config = config;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task RegisterAsync(RegisterDto dto)
        {
            var tempUser = new User { Email = dto.Email };
            var hash = _passwordHasher.HashPassword(tempUser, dto.Password);

            var p = new DynamicParameters();
            p.Add("@CustomerId", 0);
            p.Add("@UserId", dbType: System.Data.DbType.Int32, value: null);
            p.Add("@FirstName", dto.FirstName);
            p.Add("@LastName", dto.LastName);
            p.Add("@Email", dto.Email);
            p.Add("@PhoneNumber", null);
            p.Add("@WhatsAppNumber", null);
            p.Add("@CompanyName", null);
            p.Add("@JobTitle", null);
            p.Add("@Website", null);
            p.Add("@Instagram", null);
            p.Add("@LinkedIn", null);
            p.Add("@Facebook", null);
            p.Add("@Bio", null);
            p.Add("@ProfileImageUrl", null);
            p.Add("@PasswordHash", hash);

            await _repo.CreateUserAndCustomerAsync(p);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation($"=== [LoginAsync START] - Email: {dto.Email} ===");

            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning($"Login failed: User not found or inactive - Email: {dto.Email}");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            _logger.LogInformation($"User found - UserId: {user.UserId}, Role: {user.Role}, IsActive: {user.IsActive}");

            var newHash1 = _passwordHasher.HashPassword(user, dto.Password);
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", dto.Password);
            _logger.LogInformation($"Password verification result: {result}");

            if (result == PasswordVerificationResult.Failed)
            {
                if (user.PasswordHash == dto.Password)
                {
                    _logger.LogInformation("Legacy plain text password detected, rehashing...");
                    var tempUser = new User { Email = user.Email };
                    var newHash = _passwordHasher.HashPassword(tempUser, dto.Password);
                    await _repo.UpdatePasswordHashAsync(user.UserId, newHash);
                }
                else
                {
                    _logger.LogWarning($"Password verification failed for user: {dto.Email}");
                    throw new UnauthorizedAccessException("Invalid credentials");
                }
            }
            else if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation("Password rehash needed, updating...");
                var tempUser = new User { Email = user.Email };
                var newHash = _passwordHasher.HashPassword(tempUser, dto.Password);
                await _repo.UpdatePasswordHashAsync(user.UserId, newHash);
            }

            _logger.LogInformation("Generating JWT token...");
            var (token, expires) = GenerateJwtToken(user);
            var refresh = GenerateRefreshToken(user.UserId);

            _logger.LogInformation($"Login successful - UserId: {user.UserId}, Token length: {token.Length}");
            _logger.LogInformation("=== [LoginAsync END] ===");

            return new AuthResponseDto
            {
                AccessToken = token,
                ExpiresAt = expires,
                RefreshToken = refresh,
                UserId = user.UserId,
                Role = user.Role ?? "Customer"
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) return null;
            if (!_refreshStore.TryGetValue(refreshToken, out var info)) return null;
            if (info.expires < DateTime.UtcNow) { _refreshStore.TryRemove(refreshToken, out _); return null; }

            // Fetch the real user record to preserve role and email when issuing a refreshed token.
            var user = await _repo.GetByIdAsync(info.userId);
            if (user == null) return null;

            var (token, expires) = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                AccessToken = token,
                ExpiresAt = expires,
                RefreshToken = refreshToken,
                UserId = info.userId,
                Role = user.Role ?? "Customer"
            };
        }

        private (string token, DateTime expires) GenerateJwtToken(User user)
        {
            _logger.LogInformation("=== [AuthService - GenerateJwtToken START] ===");

            // Get key from config
            var jwtKeyConfig = _config["Jwt:Key"]!;
            _logger.LogInformation($"Key from config - Length: {jwtKeyConfig.Length}");
            _logger.LogInformation($"Key from config - First 20 chars: {jwtKeyConfig.Substring(0, Math.Min(20, jwtKeyConfig.Length))}");

            var keyBytes = Encoding.UTF8.GetBytes(jwtKeyConfig);
            _logger.LogInformation($"Key bytes length: {keyBytes.Length}");

            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            _logger.LogInformation($"Signing algorithm: HmacSha256");

            var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");
            var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);
            _logger.LogInformation($"Token expires in {expiresMinutes} minutes at {expires:O}");

            // Build claims
            var claims = new List<Claim>
            {
                new Claim("sub", user.UserId.ToString()),
                new Claim("email", user.Email ?? ""),
                new Claim("role", user.Role ?? "Customer")
            };

            _logger.LogInformation($"Claims added - sub: {user.UserId}, email: {user.Email}, role: {user.Role}");

            // Create JWT token
            var jwt = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            _logger.LogInformation($"JwtSecurityToken created - Issuer: {_config["Jwt:Issuer"]}, Audience: {_config["Jwt:Audience"]}");

            // Write token to string
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            _logger.LogInformation($"Token generated - Length: {token.Length}");
            _logger.LogInformation($"Token first 100 chars: {token.Substring(0, Math.Min(100, token.Length))}");
            _logger.LogInformation($"Token contains {token.Count(c => c == '.')} dots");
            _logger.LogInformation("=== [AuthService - GenerateJwtToken END] ===");

            return (token, expires);
        }

        private string GenerateRefreshToken(int userId)
        {
            var rt = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var expires = DateTime.UtcNow.AddDays(7);
            _refreshStore[rt] = (userId, expires);
            return rt;
        }
    }
}
