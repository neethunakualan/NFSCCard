using System;

namespace NFSCCard.DTOs.Customer
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public int UserId { get; set; }
        public string? NFCCodeUniqueId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public string? Website { get; set; }
        public string? Instagram { get; set; }
        public string? LinkedIn { get; set; }
        public string? Facebook { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public IFormFile ProfileImage { get; set; }
    }
}
