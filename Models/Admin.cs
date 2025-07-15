namespace V1_2025_07.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = "Admin";    
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Add for 2FA:
        public string? OtpCode { get; set; }     // Stores the OTP
        public DateTime? OtpExpiry { get; set; } // Stores expiry timestamp
    }



}
