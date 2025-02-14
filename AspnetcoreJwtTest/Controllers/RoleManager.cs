using AspnetcoreJwtTest.Entities;
using Microsoft.AspNetCore.Identity;

namespace JwtTesting.Controllers
{
    internal class RoleManager
    {
        internal async Task FindByNameAsync(string role)
        {
            throw new NotImplementedException();
        }

        public static implicit operator RoleManager(RoleManager<AspNetRole> v)
        {
            throw new NotImplementedException();
        }
    }
}