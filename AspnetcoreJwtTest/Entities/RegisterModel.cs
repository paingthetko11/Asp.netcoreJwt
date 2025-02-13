namespace AspnetcoreJwtTest.Entities
{
    public class RegisterModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }  // User role (e.g., "Admin", "User")
    }
}
