using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspnetcoreJwtTest.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetProducts()
        {
            // Sample products data
            var products = new[] { "Product 1", "Product 2", "Product 3" };
            return Ok(products);
        }
    }
}
