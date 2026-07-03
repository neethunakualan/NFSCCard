using NFSCCard.DTOs.Auth;

namespace NFSCCard.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
    }
}
