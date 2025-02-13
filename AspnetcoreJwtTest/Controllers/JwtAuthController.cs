using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AspnetcoreJwtTest;
using AspnetcoreJwtTest.Entities;
using AspnetcoreJwtTest.Interfaces;
using AspnetcoreJwtTest.Models;
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

        public AccountController(UserManager<IdentityUser> userManager,
            ITokenBuilder tokenBuilder,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenBuilder = tokenBuilder;
            _context = context;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User model)
        {
            if (await _userManager.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest("User with this email already exists.");
            }

            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return Ok("User registered successfully.");
            }
            return BadRequest(result.Errors);
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim(ClaimTypes.NameIdentifier, user.Id) // ✅ Ensure this is set
    };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(1), // Access token expires in 1 minute
                signingCredentials: creds
            );

            var (refreshToken, refreshTokenExpiry) = GenerateRefreshToken();

            // ✅ Always update the refresh token in storage
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
                var refreshTokenExpiry = DateTime.UtcNow.AddMinutes(3); // Set refresh token to expire in 3 minutes
                return (refreshToken, refreshTokenExpiry);
            }
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
