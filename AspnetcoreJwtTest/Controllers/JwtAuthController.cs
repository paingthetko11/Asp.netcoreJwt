using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AspnetcoreJwtTest;
using AspnetcoreJwtTest.Entities;
using AspnetcoreJwtTest.Interfaces;
using AspnetcoreJwtTest.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace JwtTesting.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITokenBuilder _tokenBuilder;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<IdentityUser> userManager,
            ITokenBuilder tokenBuilder,
            ApplicationDbContext context, RoleManager<IdentityRole> roleManager,
            IConfiguration configuration, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _tokenBuilder = tokenBuilder;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _signInManager = signInManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Check if the user already exists
            if (await _userManager.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest("User with this email already exists.");

            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign the role (default "User" if not provided)
            var role = string.IsNullOrEmpty(model.Role) ? "User" : model.Role;
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);

            return Ok(new { message = "User registered successfully." });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);

            if (user == null || user.UserName == null)
            {
                return Unauthorized("Invalid username or password.");
            }


            var (token, refreshToken, refreshTokenExpiry) = await GenerateJwtToken(user);

            return Ok(new { token, refreshToken, refreshTokenExpiry });

        }

        private async Task<(string, string, DateTime)> GenerateJwtToken(IdentityUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(1),
                signingCredentials: creds
            );

            var (refreshToken, refreshTokenExpiry) = GenerateRefreshToken();
            await _userManager.SetAuthenticationTokenAsync(user, "MyApp", "RefreshToken", refreshToken);
            await _userManager.SetAuthenticationTokenAsync(user, "MyApp", "RefreshTokenExpiry", refreshTokenExpiry.ToString());

            return (new JwtSecurityTokenHandler().WriteToken(token), refreshToken, refreshTokenExpiry);
        }

        private (string, DateTime) GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);
                var refreshTokenExpiry = DateTime.UtcNow.AddMinutes(3);
                return (refreshToken, refreshTokenExpiry);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok("Welcome, Admin!");
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequest tokenRequest) // Ensure this matches your model
        {
            if (tokenRequest == null || string.IsNullOrEmpty(tokenRequest.RefreshToken) || string.IsNullOrEmpty(tokenRequest.AccessToken))
            {
                return BadRequest("Invalid client request: Missing tokens.");
            }

            var principal = GetPrincipalFromExpiredToken(tokenRequest.AccessToken);
            if (principal == null)
            {
                return BadRequest("Invalid client request: Could not extract claims.");
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value; // FIXED: Use NameIdentifier claim
            if (userId == null)
            {
                return BadRequest("Invalid client request: User ID missing.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("Invalid client request: User not found.");
            }

            if (!await ValidateRefreshToken(user, tokenRequest.RefreshToken))
            {
                return BadRequest("Invalid client request: Refresh token invalid or expired.");
            }

            // Generate new tokens
            var (newAccessToken, newRefreshToken, newRefreshTokenExpiry) = await GenerateJwtToken(user);

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiry = newRefreshTokenExpiry
            });
        }
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found"))),
                    ValidateLifetime = false // Allow expired tokens
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtToken || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null; // Invalid token
                }

                // ✅ Check if the token is expired
                var expiryClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp);
                if (expiryClaim != null && long.TryParse(expiryClaim.Value, out long expiry))
                {
                    var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expiry).UtcDateTime;
                    if (expiryDate > DateTime.UtcNow)
                    {
                        return null; // Token is still valid, no need to refresh
                    }
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }


        private async Task<bool> ValidateRefreshToken(IdentityUser user, string refreshToken)
        {
            var storedRefreshToken = await _userManager.GetAuthenticationTokenAsync(user, "MyApp", "RefreshToken");
            var storedRefreshTokenExpiry = await _userManager.GetAuthenticationTokenAsync(user, "MyApp", "RefreshTokenExpiry");

            if (string.IsNullOrEmpty(storedRefreshToken) || string.IsNullOrEmpty(storedRefreshTokenExpiry))
            {
                return false;
            }

            // ✅ Ensure refresh token is not expired
            if (!DateTime.TryParse(storedRefreshTokenExpiry, out DateTime expiryDate) || expiryDate < DateTime.UtcNow)
            {
                return false; // Expired
            }

            return storedRefreshToken == refreshToken;
        }
    }
}