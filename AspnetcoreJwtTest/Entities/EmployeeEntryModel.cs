
namespace AspnetcoreJwtTest.Entities
{
    public class EmployeeEntryModel
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public DateTime? DateOfBirth { get; internal set; }
        public string? Gender { get; internal set; }
    }
}
