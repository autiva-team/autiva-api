using Microsoft.AspNetCore.Mvc;

namespace Autiva.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var cadena = _configuration.GetConnectionString("DefaultConnection");

            return Ok(new
            {
                estado = "ok",
                mensaje = "API Autiva v1",
                conexionConfigurada = !string.IsNullOrEmpty(cadena),
                fecha = DateTime.Now
            });
        }
    }
}