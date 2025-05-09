using Microsoft.AspNetCore.Mvc;

using CW_7_s31270.Models;
using CW_7_s31270.Services;

namespace CW_7_s31270.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public ClientsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        /// <summary>
        /// Pobiera wszystkie wycieczki powiązane z danym klientem
        /// </summary>
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            try
            {
                bool clientExists = await _dbService.ClientExistsAsync(id);
                if (!clientExists)
                {
                    return NotFound($"Klient o ID {id} nie został znaleziony");
                }

                var trips = await _dbService.GetClientTripsAsync(id);
                
                if (!trips.Any())
                {
                    return Ok(new { message = $"Klient o ID {id} nie jest zarejestrowany na żadne wycieczki" });
                }

                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tworzy nowy rekord klienta
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] Client client)
        {
            if (client == null)
            {
                return BadRequest("Dane klienta są wymagane");
            }
            
            if (string.IsNullOrEmpty(client.FirstName))
            {
                return BadRequest("Imię jest wymagane");
            }
            if (string.IsNullOrEmpty(client.LastName))
            {
                return BadRequest("Nazwisko jest wymagane");
            }
            if (string.IsNullOrEmpty(client.Email))
            {
                return BadRequest("Email jest wymagany");
            }
            
            if (!client.Email.Contains("@") || !client.Email.Contains("."))
            {
                return BadRequest("Nieprawidłowy format adresu email");
            }

            try
            {
                int newClientId = await _dbService.CreateClientAsync(client);
                return CreatedAtAction(nameof(GetClientTrips), new { id = newClientId }, 
                    new { IdClient = newClientId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }

        /// <summary>
        /// Rejestruje klienta na wybraną wycieczkę
        /// </summary>
        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
        {
            try
            {
                if (!await _dbService.ClientExistsAsync(id))
                {
                    return NotFound($"Klient o ID {id} nie został znaleziony");
                }
                
                if (!await _dbService.TripExistsAsync(tripId))
                {
                    return NotFound($"Wycieczka o ID {tripId} nie została znaleziona");
                }
                
                if (await _dbService.IsClientRegisteredForTripAsync(id, tripId))
                {
                    return Conflict($"Klient jest już zarejestrowany na tę wycieczkę");
                }
                
                var (maxPeople, currentParticipants) = await _dbService.GetTripCapacityInfoAsync(tripId);
                
                if (currentParticipants >= maxPeople)
                {
                    return BadRequest("Osiągnięto maksymalną liczbę uczestników dla tej wycieczki");
                }
                
                await _dbService.RegisterClientForTripAsync(id, tripId);
                
                return Ok(new { message = "Klient został pomyślnie zarejestrowany na wycieczkę" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }

        /// <summary>
        /// Usuwa rejestrację klienta z wycieczki
        /// </summary>
        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
        {
            try
            {
                if (!await _dbService.IsClientRegisteredForTripAsync(id, tripId))
                {
                    return NotFound("Rejestracja nie została znaleziona");
                }
                
                await _dbService.UnregisterClientFromTripAsync(id, tripId);
                
                return Ok(new { message = "Rejestracja klienta została pomyślnie usunięta" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd serwera: {ex.Message}");
            }
        }
    }
}
