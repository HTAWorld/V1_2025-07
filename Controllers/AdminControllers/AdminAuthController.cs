﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using V1_2025_07.Models;
using V1_2025_07.Services;

namespace V1_2025_07.Controllers
{
    [Route("api/admin/auth")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailSender _emailSender;

        public AdminAuthController(ApplicationDbContext context, IConfiguration config, EmailSender emailSender)
        {
            _context = context;
            _config = config;
            _emailSender = emailSender;
        }

        // Step 1: Admin Login (generate & email OTP)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Email and password are required.");

            var admin = await _context.Admins.FirstOrDefaultAsync(a =>
                a.Email == dto.Email && a.IsActive && !a.IsDeleted);

            if (admin == null || !VerifyPassword(dto.Password, admin.PasswordHash))
                return Unauthorized("Invalid credentials");

            // Generate random 6-digit code
            var code = new Random().Next(100000, 999999).ToString();

            admin.OtpCode = code;
            admin.OtpExpiry = DateTime.UtcNow.AddMinutes(5); // 5 minutes validity
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(dto.Email, "Your Admin 2FA Code", $"Your admin 2FA code is: {code}");

            return Ok(new { message = "2FA code sent to your email. Please verify to complete login." });
        }

        // Step 2: Verify OTP and issue JWT
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Admin2FADto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Code))
                return BadRequest("Email and code are required.");

            var admin = await _context.Admins.FirstOrDefaultAsync(a =>
                a.Email == dto.Email && a.IsActive && !a.IsDeleted);

            if (admin == null)
                return Unauthorized("Invalid email.");

            // Check OTP and expiry
            if (admin.OtpCode != dto.Code || admin.OtpExpiry == null || admin.OtpExpiry < DateTime.UtcNow)
                return Unauthorized("Invalid or expired 2FA code");

            // Invalidate OTP after successful login
            admin.OtpCode = null;
            admin.OtpExpiry = null;
            admin.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwt(admin);

            return Ok(new { token });
        }

        ////======================me profile========================

        //[HttpGet("me")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> GetMyProfile()
        //{
        //    // Extract claims
        //    var sub = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        //    var email = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        //    var expStr = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

        //    if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
        //        return Unauthorized("Invalid token.");

        //    if (!int.TryParse(sub, out int adminId))
        //        return Unauthorized("Invalid admin ID in token.");

        //    // Fetch admin data from DB
        //    var admin = await _context.Admins
        //        .Where(a => a.Id == adminId && a.IsActive && !a.IsDeleted)
        //        .Select(a => new {
        //            id = a.Id,
        //            username = a.Username,
        //            role = a.Role // Adjust to your actual property
        //        })
        //        .FirstOrDefaultAsync();

        //    if (admin == null)
        //        return NotFound("Admin not found.");

        //    // Parse expiry if present
        //    long? exp = null;
        //    if (!string.IsNullOrEmpty(expStr) && long.TryParse(expStr, out var expParsed))
        //        exp = expParsed;

        //    // Build JSON result
        //    var result = new
        //    {
        //        sub = adminId,
        //        email = email,
        //        admin = admin,
        //        exp = exp
        //    };

        //    return Ok(result);
        //}


        //// ================= GET /me ================= working
        //[HttpGet("me")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> GetMyProfile()
        //{
        //    // Get the admin ID from 'sub' claim (or NameIdentifier)
        //    var sub = User.Claims.FirstOrDefault(c =>
        //        c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier
        //    )?.Value;

        //    if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out int adminId))
        //        return Unauthorized("Invalid admin ID in token.");

        //    // Fetch admin data from DB
        //    var admin = await _context.Admins
        //        .Where(a => a.Id == adminId && a.IsActive && !a.IsDeleted)
        //        .Select(a => new
        //        {
        //            id = a.Id,
        //            username = a.Username,
        //            role = a.Role
        //        })
        //        .FirstOrDefaultAsync();

        //    if (admin == null)
        //        return NotFound("Admin not found.");

        //    return Ok(admin);
        //}


        [HttpGet("me")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Extract claims
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                     ?? User.FindFirst(ClaimTypes.Email)?.Value;
            var expStr = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value
                      ?? User.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
                return Unauthorized("Invalid token.");

            if (!int.TryParse(sub, out int adminId))
                return Unauthorized("Invalid admin ID in token.");

            // Fetch admin data from DB
            var admin = await _context.Admins
                .Where(a => a.Id == adminId && a.IsActive && !a.IsDeleted)
                .Select(a => new
                {
                    id = a.Id,
                    username = a.Username,
                    role = a.Role
                })
                .FirstOrDefaultAsync();

            if (admin == null)
                return NotFound("Admin not found.");

            // Parse expiry if present
            long? exp = null;
            if (!string.IsNullOrEmpty(expStr) && long.TryParse(expStr, out var expParsed))
                exp = expParsed;

            // Build JSON result as you want
            var result = new
            {
                sub = adminId,
                email = email,
                admin = admin,
                exp = exp
            };

            return Ok(result);
        }






        // ===== Helper Methods =====

        private string GenerateJwt(Admin admin)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"] ?? throw new Exception("JWT secret not set")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, admin.Email),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("username", admin.Username)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // PBKDF2 password verification
        private static bool VerifyPassword(string password, string storedHash)
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


        // DTOs
        public class AdminLoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        public class Admin2FADto
        {
            public string Email { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }
}
