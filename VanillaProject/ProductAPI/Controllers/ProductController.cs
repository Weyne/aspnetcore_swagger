using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ProductAPI.Controllers
{
    [ApiController]
    [EnableCors("cors")]
    [Authorize]
    [Route("v{version:apiVersion}/[Controller]")]
    [ApiVersion("1.0")]
    public class ProductController : Controller
    {
        private static readonly string[] Summaries = new[]
        {
        "Product #1", "Product #2", "Product #3"
        };

        [MapToApiVersion("1.0")]
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(Summaries);
        }
    }
}
