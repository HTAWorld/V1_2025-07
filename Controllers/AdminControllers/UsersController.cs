using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using V1_2025_07.Models;

namespace V1_2025_07.Controllers.AdminControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===========================
        // Social Login (Google demo)
        // ===========================
        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Provider) || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("Provider and token are required.");

            SocialUserInfo? socialUser = null;

            if (dto.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                socialUser = await ValidateGoogleToken(dto.Token);
            }
            else
            {
                return BadRequest("Unsupported provider. Supported: Google");
            }

            if (socialUser == null)
                return Unauthorized("Invalid social token.");

            // Lookup user by provider id
            User? user = null;
            if (dto.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == socialUser.Id);
            }

            // Create user if not found
            if (user == null)
            {
                var ua = Request.Headers.UserAgent.ToString();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                user = new User
                {
                    Email = socialUser.Email,
                    Username = string.IsNullOrWhiteSpace(socialUser.Name) ? socialUser.Email : socialUser.Name,
                    IsActive = true,
                    Role = "Player",
                    CreatedAt = DateTime.UtcNow,
                    GoogleId = socialUser.Id,

                    // ensure NOT NULL columns have values
                    LastLoginAt = DateTime.UtcNow,
                    LastLoginIP = string.IsNullOrWhiteSpace(ip) ? "0.0.0.0" : ip,
                    LastLoginDevice = string.IsNullOrWhiteSpace(ua) ? "Unknown" : ua
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update last login fields
                var ua = Request.Headers.UserAgent.ToString();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                user.LastLoginAt = DateTime.UtcNow;
                user.LastLoginIP = string.IsNullOrWhiteSpace(ip) ? (user.LastLoginIP ?? "0.0.0.0") : ip;
                user.LastLoginDevice = string.IsNullOrWhiteSpace(ua) ? (user.LastLoginDevice ?? "Unknown") : ua;

                await _context.SaveChangesAsync();
            }

            // Token issuing left as a stub (replace with your real token service)
            var token = $"FAKE-JWT-FOR-{user.Username}";

            var result = new UserOutputDto(user);
            return Ok(new { token, user = result });
        }

        private async Task<SocialUserInfo?> ValidateGoogleToken(string idToken)
        {
            try
            {
                var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";
                using var http = new HttpClient();
                var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("sub", out var subElem)) return null;

                var email = doc.RootElement.TryGetProperty("email", out var emailElem) ? emailElem.GetString() : null;
                if (string.IsNullOrWhiteSpace(email)) return null;

                var name = doc.RootElement.TryGetProperty("name", out var nElem) ? nElem.GetString() : email;

                return new SocialUserInfo
                {
                    Id = subElem.GetString() ?? "",
                    Email = email!,
                    Name = name ?? email!
                };
            }
            catch
            {
                return null;
            }
        }

        // ===========================
        // Admin/List endpoints
        // ===========================
        // GET: api/users?includeDeleted=false
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserOutputDto>>> GetUsers([FromQuery] bool includeDeleted = false)
        {
            var q = _context.Users.AsQueryable();
            if (!includeDeleted) q = q.Where(u => !u.IsDeleted);

            var users = await q.OrderByDescending(u => u.CreatedAt)
                               .Select(u => new UserOutputDto(u))
                               .ToListAsync();
            return users;
        }

        // GET: api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserOutputDto>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();
            return new UserOutputDto(user);
        }

        // ===========================
        // CREATE (expects hashed password in payload)
        // ===========================
        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserOutputDto>> CreateUser(UserInputDto input)
        {
            if (input == null) return BadRequest("Payload is required.");

            // Required basics
            if (string.IsNullOrWhiteSpace(input.Username)) return BadRequest("Username is required.");
            if (string.IsNullOrWhiteSpace(input.Email)) return BadRequest("Email is required.");
            if (string.IsNullOrWhiteSpace(input.Password)) return BadRequest("Password (hashed) is required.");

            // Enforce hashed password format "salt:hash"
            if (!IsValidHashedPassword(input.Password))
                return BadRequest("Password must be a hashed string in the format 'saltBase64:hashBase64'.");

            // Unique constraints
            if (await _context.Users.AnyAsync(u => u.Email == input.Email))
                return BadRequest("Email already exists.");
            if (await _context.Users.AnyAsync(u => u.Username == input.Username))
                return BadRequest("Username already exists.");

            var ua = Request.Headers.UserAgent.ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var user = new User
            {
                Username = input.Username,
                Email = input.Email,
                PasswordHash = input.Password, // already hashed by client
                IsActive = input.IsActive,
                Role = input.Role ?? "Player",
                GoogleId = input.GoogleId,
                FacebookId = input.FacebookId,
                AppleId = input.AppleId,
                MobileNumber = input.MobileNumber,
                IsMobileVerified = input.IsMobileVerified,
                KycDocumentType = input.KycDocumentType,
                KycDocumentNumber = input.KycDocumentNumber,
                KycDocumentUrl = input.KycDocumentUrl,
                ReferralCode = input.ReferralCode,
                ReferredBy = input.ReferredBy,
                CreatedAt = DateTime.UtcNow,
                UserStatus = input.UserStatus ?? "Active",

                // prevent NOT NULL errors on insert
                LastLoginAt = null,
                LastLoginIP = string.IsNullOrWhiteSpace(ip) ? "0.0.0.0" : ip,
                LastLoginDevice = string.IsNullOrWhiteSpace(ua) ? "Unknown" : ua,

                // sensible defaults
                IsKycVerified = false,
                KycVerifiedAt = null,
                IsDeleted = false,
                DeletedAt = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new UserOutputDto(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, dto);
        }

        // ===========================
        // UPDATE
        // ===========================
        // PUT: api/users/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, UserInputDto input)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            // Optional: if email is changed, check uniqueness
            if (!string.IsNullOrWhiteSpace(input.Email) && !string.Equals(input.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _context.Users.AnyAsync(u => u.Email == input.Email && u.Id != id);
                if (exists) return BadRequest("Email already exists.");
                user.Email = input.Email;
            }

            // Optional: if username is changed, check uniqueness
            if (!string.IsNullOrWhiteSpace(input.Username) && !string.Equals(input.Username, user.Username, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _context.Users.AnyAsync(u => u.Username == input.Username && u.Id != id);
                if (exists) return BadRequest("Username already exists.");
                user.Username = input.Username;
            }

            // Only update password if provided (expects hashed)
            if (!string.IsNullOrWhiteSpace(input.Password))
            {
                if (!IsValidHashedPassword(input.Password))
                    return BadRequest("Password must be a hashed string in the format 'saltBase64:hashBase64'.");
                user.PasswordHash = input.Password;
            }

            user.IsActive = input.IsActive;
            user.Role = input.Role ?? user.Role;
            user.GoogleId = input.GoogleId;
            user.FacebookId = input.FacebookId;
            user.AppleId = input.AppleId;
            user.MobileNumber = input.MobileNumber;
            user.IsMobileVerified = input.IsMobileVerified;
            user.KycDocumentType = input.KycDocumentType;
            user.KycDocumentNumber = input.KycDocumentNumber;
            user.KycDocumentUrl = input.KycDocumentUrl;
            user.ReferralCode = input.ReferralCode;
            user.ReferredBy = input.ReferredBy;
            user.UserStatus = input.UserStatus ?? user.UserStatus;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ===========================
        // SOFT DELETE
        // ===========================
        // DELETE: api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ===========================
        // KYC APPROVE
        // ===========================
        // POST: api/users/5/kyc-verify
        [HttpPost("{id:int}/kyc-verify")]
        public async Task<IActionResult> ApproveKyc(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.IsKycVerified = true;
            user.KycVerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ===========================
        // RESET PASSWORD (expects hashed password in body)
        // ===========================
        // POST: api/users/5/reset-password
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] PasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("NewPassword (hashed) is required.");

            if (!IsValidHashedPassword(dto.NewPassword))
                return BadRequest("NewPassword must be a hashed string in the format 'saltBase64:hashBase64'.");

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.PasswordHash = dto.NewPassword;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ===========================
        // Helpers
        // ===========================
        private static bool IsValidHashedPassword(string candidate)
        {
            // Enforce "salt:hash" where both are Base64
            var parts = candidate.Split(':');
            if (parts.Length != 2) return false;
            return IsBase64(parts[0]) && IsBase64(parts[1]);
        }

        private static bool IsBase64(string s)
        {
            try
            {
                _ = Convert.FromBase64String(s);
                return true;
            }
            catch { return false; }
        }
    }

    // ===========================
    // DTOs
    // ===========================
    public class UserInputDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        // Expecting already hashed value from client: "saltBase64:hashBase64"
        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Role { get; set; } = "Player";

        public string? GoogleId { get; set; }
        public string? FacebookId { get; set; }
        public string? AppleId { get; set; }

        public string? MobileNumber { get; set; }
        public bool IsMobileVerified { get; set; }

        public string? KycDocumentType { get; set; }
        public string? KycDocumentNumber { get; set; }
        public string? KycDocumentUrl { get; set; }

        public string? ReferralCode { get; set; }
        public string? ReferredBy { get; set; }

        public string? UserStatus { get; set; }
    }

    public class PasswordDto
    {
        // Expecting already hashed value from client: "saltBase64:hashBase64"
        public string NewPassword { get; set; } = default!;
    }

    public class SocialLoginDto
    {
        public string Provider { get; set; } = default!; // e.g., "Google"
        public string Token { get; set; } = default!;    // ID token from provider
    }

    public class SocialUserInfo
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Name { get; set; }
    }

    public record UserOutputDto
    {
        public int Id { get; init; }
        public string Username { get; init; } = "";
        public string Email { get; init; } = "";
        public string Role { get; init; } = "Player";
        public bool IsActive { get; init; }
        public string? GoogleId { get; init; }
        public string? FacebookId { get; init; }
        public string? AppleId { get; init; }
        public string? MobileNumber { get; init; }
        public bool IsMobileVerified { get; init; }
        public string? KycDocumentType { get; init; }
        public string? KycDocumentNumber { get; init; }
        public string? KycDocumentUrl { get; init; }
        public bool IsKycVerified { get; init; }
        public DateTime? KycVerifiedAt { get; init; }
        public bool IsDeleted { get; init; }
        public DateTime? DeletedAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public string? LastLoginIP { get; init; }
        public string? LastLoginDevice { get; init; }
        public string? UserStatus { get; init; }

        public UserOutputDto() { }

        public UserOutputDto(User u)
        {
            Id = u.Id;
            Username = u.Username;
            Email = u.Email;
            Role = u.Role;
            IsActive = u.IsActive;
            GoogleId = u.GoogleId;
            FacebookId = u.FacebookId;
            AppleId = u.AppleId;
            MobileNumber = u.MobileNumber;
            IsMobileVerified = u.IsMobileVerified;
            KycDocumentType = u.KycDocumentType;
            KycDocumentNumber = u.KycDocumentNumber;
            KycDocumentUrl = u.KycDocumentUrl;
            IsKycVerified = u.IsKycVerified;
            KycVerifiedAt = u.KycVerifiedAt;
            IsDeleted = u.IsDeleted;
            DeletedAt = u.DeletedAt;
            CreatedAt = u.CreatedAt;
            LastLoginAt = u.LastLoginAt;
            LastLoginIP = u.LastLoginIP;
            LastLoginDevice = u.LastLoginDevice;
            UserStatus = u.UserStatus;
        }
    }
}
