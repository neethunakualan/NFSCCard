namespace NFSCCard.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = null!;
    }
}
