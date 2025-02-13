using Microsoft.AspNetCore.Identity;

public class UserRefreshToken
{
    public int Id { get; set; } // Primary key for the refresh token

    public string UserId { get; set; } // User ID linked to the refresh token
    public IdentityUser User { get; set; } // Navigation property to the associated IdentityUser

    public string RefreshToken { get; set; } // The actual refresh token string
    public DateTime ExpiryDate { get; set; } // Expiry date of the refresh token
}
