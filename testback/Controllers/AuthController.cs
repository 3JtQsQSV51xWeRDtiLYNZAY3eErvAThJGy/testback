
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using testback.Data;
using testback.Models;

namespace testback.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public AuthController(IConfiguration config, AppDbContext db)
        {
            _config = config;
            _db = db;
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string ConfirmPassword { get; set; } = null!;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username and password are required.");

            if (req.Password != req.ConfirmPassword)
                return BadRequest("Password and ConfirmPassword do not match.");

            var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
            if (exists)
                return Conflict("Username already exists.");

            var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var user = new User
            {
                Username = req.Username,
                PasswordHash = hash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Profile), new { id = user.UserId }, new { user.UserId, user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                return Unauthorized();

            var verified = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!verified)
                return Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(new { token = tokenString, username = user.Username });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound();

            return Ok(new { user.UserId, user.Username, user.CreatedAt, user.IsActive });
        }
    }
}
