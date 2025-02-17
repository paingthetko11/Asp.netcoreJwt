using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AuthController> _logger;
    private readonly SmtpSettings _smtpSettings;

    public AuthController(UserManager<IdentityUser> userManager, ILogger<AuthController> logger, SmtpSettings smtpSettings)
    {
        _userManager = userManager;
        _logger = logger;
        _smtpSettings = smtpSettings;
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Invalid request.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest("No account found with that email address.");
        }

        _logger.LogInformation("Received password reset request for {Email}", request.Email);

        // Verify token without decoding
        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword", request.Token);
        if (!isValid)
        {
            _logger.LogWarning("Invalid or expired reset token for {Email}", request.Email);
            return BadRequest("Invalid or expired token.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Password reset failed for {Email}: {Errors}", request.Email, errors);
            return BadRequest($"Password reset failed: {errors}");
        }

        _logger.LogInformation("Password reset successful for {Email}", request.Email);
        return Ok("Password reset successful.");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Invalid request.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest("No account found with that email address.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(token); // Encode for URL safety
        var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={encodedToken}&email={WebUtility.UrlEncode(request.Email)}";

        await SendResetEmail(request.Email, resetLink);

        _logger.LogInformation("Password reset email sent to {Email}", request.Email);
        return Ok("Password reset email sent.");
    }

    private async Task SendResetEmail(string email, string resetLink)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpSettings.Host)
            {
                Port = _smtpSettings.Port,
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Username),
                Subject = "Password Reset Request",
                Body = $"Click here to reset your password: <a href=\"{HtmlEncoder.Default.Encode(resetLink)}\">Reset Password</a>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error sending reset email to {Email}: {Exception}", email, ex.Message);
        }
    }
}


