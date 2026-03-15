using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public ActionResult<object> Get()
        {
            return Ok(new
            {
                success = true,
                message = "FrontCRM API is running",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }
    }
}
