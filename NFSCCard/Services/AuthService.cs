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

namespace NFSCCard.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _passwordHasher;

        // Simple in-memory refresh store (replace with DB)
        private static readonly ConcurrentDictionary<string, (int userId, DateTime expires)> _refreshStore = new();

        public AuthService(IAuthRepository repo, IConfiguration config, IPasswordHasher<User> passwordHasher)
        {
            _repo = repo;
            _config = config;
            _passwordHasher = passwordHasher;
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
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid credentials");
            var newHash1 = _passwordHasher.HashPassword(user, dto.Password);
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                if (user.PasswordHash == dto.Password)
                {
                    // Legacy support: password stored in plain text. Re-hash it on first successful login.
                    var tempUser = new User { Email = user.Email };
                    var newHash = _passwordHasher.HashPassword(tempUser, dto.Password);
                    await _repo.UpdatePasswordHashAsync(user.UserId, newHash);
                }
                else
                {
                    throw new UnauthorizedAccessException("Invalid credentials");
                }
            }
            else if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var tempUser = new User { Email = user.Email };
                var newHash = _passwordHasher.HashPassword(tempUser, dto.Password);
                await _repo.UpdatePasswordHashAsync(user.UserId, newHash);
            }

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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");
            var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };

            var jwt = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
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
