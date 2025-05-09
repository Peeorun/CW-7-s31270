using CW_7_s31270.Services;
using Microsoft.AspNetCore.Mvc;

namespace CW_7_s31270.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public TripsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        /// <summary>
        /// Pobiera wszystkie dostępne wycieczki wraz z informacjami o krajach
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            try
            {
                var trips = await _dbService.GetTripsAsync();
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }
    }
}
