using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AspnetcoreJwtTest.Interfaces
{
    public interface ITokenBuilder
    {
        /// <summary>
		/// Generate Access Token
		/// </summary>
		/// <remarks>
		/// Change Identity User Default UserName Value to UserId For Global Value
		/// - new Claim(ClaimTypes.Name, user.UserName) -
		/// If you want to change this value to default Username, set the paramenter to 'user.Id'
		/// 
		/// Token Expiry is 1 Day
		/// </remarks>
		/// <code>
		/// new Claim(ClaimTypes.Name, user.UserName),
		/// </code>
		/// <param name="user"></param>
		/// <param name="Role"></param>
		/// <param name="expiry"></param>
		/// <returns></returns>
		public string GenerateAccessToken(IdentityUser user, DateTime expiry);
        /// <summary>
        /// Generate Refresh Token
        /// Simple RandomNumber Generateor 64bit
        /// </summary>
        /// <returns></returns>
        public string GenerateRefreshToken();
        /// <summary>
        /// Get ClaimsPrincipal from Expired Token
        /// </summary>
        /// <param name="access_token"></param>
        /// <returns></returns>
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string access_token);
    }
}
