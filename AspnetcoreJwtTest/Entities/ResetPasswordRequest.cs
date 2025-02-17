namespace AspnetcoreJwtTest.Entities
{
    public class ResetPasswordRequest
    {
        public required string Email { get; set; }
        public required string Token { get; set; }  // The reset token sent to the user
        public required string NewPassword { get; set; }
    }
}
