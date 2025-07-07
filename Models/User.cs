namespace V1_2025_07.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // For password: store hashed password only!
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public string Role { get; set; } = "Player"; // Or "Admin", "Moderator", etc.

        // Social Login IDs
        public string GoogleId { get; set; }
        public string FacebookId { get; set; }
        public string AppleId { get; set; }

        // Mobile Login
        public string MobileNumber { get; set; }
        public bool IsMobileVerified { get; set; } = false;

        // KYC Fields
        public string KycDocumentType { get; set; } // Aadhaar, PAN, etc.
        public string KycDocumentNumber { get; set; }
        public string KycDocumentUrl { get; set; }
        public bool IsKycVerified { get; set; } = false;
        public DateTime? KycVerifiedAt { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Referral
        public string ReferralCode { get; set; }
        public string ReferredBy { get; set; } // Store referring user's code or Id

        // Audit Trail
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string LastLoginIP { get; set; }
        public string LastLoginDevice { get; set; }

        // User Status
        public string UserStatus { get; set; } = "Active"; // "Active", "Banned", etc.
    }


}
