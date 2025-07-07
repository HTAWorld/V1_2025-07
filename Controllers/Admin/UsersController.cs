using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using V1_2025_07;
using V1_2025_07.Models;

namespace V1_2025_07.Controllers.Admin
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

        // ========== SOCIAL LOGIN ENDPOINT ==========

        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginDto dto)
        {
            // Step 1: Validate the social token (example: Google)
            SocialUserInfo socialUser = null;
            if (dto.Provider == "Google")
            {
                socialUser = await ValidateGoogleToken(dto.Token);
            }
            // Extend here: else if (dto.Provider == "Facebook") {...}

            if (socialUser == null)
                return Unauthorized("Invalid social token");

            // Step 2: Lookup user by social id
            User user = null;
            if (dto.Provider == "Google")
                user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == socialUser.Id);

            // Step 3: If user not found, create new user
            if (user == null)
            {
                user = new User
                {
                    Email = socialUser.Email,
                    Username = socialUser.Name ?? socialUser.Email,
                    IsActive = true,
                    Role = "Player",
                    CreatedAt = DateTime.UtcNow,
                    GoogleId = socialUser.Id
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Step 4: (Optional) Generate JWT/token (not implemented here, stub)
            var token = $"FAKE-JWT-FOR-{user.Username}"; // Replace with real JWT logic

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role,
                    user.IsActive
                }
            });
        }

        // Helper: Validate Google Token
        private async Task<SocialUserInfo> ValidateGoogleToken(string idToken)
        {
            // Google's tokeninfo endpoint
            var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";
            using var http = new HttpClient();
            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("sub", out var subElem)) return null;

            var email = doc.RootElement.GetProperty("email").GetString();
            var name = doc.RootElement.TryGetProperty("name", out var nElem) ? nElem.GetString() : email;

            return new SocialUserInfo
            {
                Id = subElem.GetString(),
                Email = email,
                Name = name
            };
        }

        // ========== ADMIN ENDPOINTS ==========

        // GET: api/users?includeDeleted=false
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] bool includeDeleted = false)
        {
            var query = _context.Users.AsQueryable();
            if (!includeDeleted)
                query = query.Where(u => !u.IsDeleted);
            return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();
            return user;
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(UserInputDto input)
        {
            if (await _context.Users.AnyAsync(u => u.Email == input.Email))
                return BadRequest("Email already exists.");

            var user = new User
            {
                Username = input.Username,
                Email = input.Email,
                PasswordHash = HashPassword(input.Password),
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
                UserStatus = input.UserStatus ?? "Active"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserInputDto input)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.Username = input.Username;
            user.Email = input.Email;
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

            if (!string.IsNullOrWhiteSpace(input.Password))
                user.PasswordHash = HashPassword(input.Password);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/users/5 (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/users/5/kyc-verify
        [HttpPost("{id}/kyc-verify")]
        public async Task<IActionResult> ApproveKyc(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.IsKycVerified = true;
            user.KycVerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: api/users/5/reset-password
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] PasswordDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsDeleted) return NotFound();

            user.PasswordHash = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // =============== PASSWORD HASHING HELPERS ===============

        private static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] stored = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] computed = pbkdf2.GetBytes(32);

            return stored.SequenceEqual(computed);
        }
    }

    // =========== DTOs ===========

    public class UserInputDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Only for creation/update
        public bool IsActive { get; set; } = true;
        public string Role { get; set; } = "Player";
        public string GoogleId { get; set; }
        public string FacebookId { get; set; }
        public string AppleId { get; set; }
        public string MobileNumber { get; set; }
        public bool IsMobileVerified { get; set; }
        public string KycDocumentType { get; set; }
        public string KycDocumentNumber { get; set; }
        public string KycDocumentUrl { get; set; }
        public string ReferralCode { get; set; }
        public string ReferredBy { get; set; }
        public string UserStatus { get; set; }
    }

    public class PasswordDto
    {
        public string NewPassword { get; set; }
    }

    public class SocialLoginDto
    {
        public string Provider { get; set; } // "Google", "Facebook", "Apple"
        public string Token { get; set; }    // ID token from provider
    }

    public class SocialUserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }
}
