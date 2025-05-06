using System.ClientModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Exceptions;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;
        private readonly IClientService _clientService;

        public TripsController(ITripsService tripsService, IClientService clientService)
        {
            _tripsService = tripsService;
            _clientService = clientService;
        }
        
        [HttpGet("/api/trips")]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }

        [HttpGet("~/api/clients/{id}/trips")]
        public async Task<IActionResult> GetClientsTrips(int id)
        {
            try
            {
                var trips = await _clientService.GetClientsTrips(id);
                if (trips == null || !trips.Any())
                {
                    throw new ClientHasNoTripsException(id);
                }
                return Ok(trips);
            }
            catch (ClientHasNoTripsException e)
            {
                return NotFound(new { message = e.Message });
            }
        }
        
    }
}
