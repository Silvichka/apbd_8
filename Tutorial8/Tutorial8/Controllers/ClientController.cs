using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers{

    [Microsoft.AspNetCore.Components.Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }
        
        [HttpPost("~/api/clients")]
        public async Task<IActionResult> addClient([FromBody] ClientDTO clientDto)
        {
            try
            {
                var id = await _clientService.CreateNewClient(clientDto);
                return Created($"api/clients/{id}", new {id});
            }
            catch (CreatingClientException e)
            {
                return BadRequest(new { message = e.Message });
            }
            
        }

        [HttpPut("/api/clients/{clientId}/trips/{tripId}")]
        public async Task<IActionResult> assignClientToTrip(int clientId, int tripId)
        {
            var message = await _clientService.assignClientToTrip(clientId, tripId); 
            return Ok(message);
            
        }

        [HttpDelete("/api/clients/{clientId}/trips/{tripId}")]
        public async Task<IActionResult> deleteClientFromTrip(int clientId, int tripId)
        {
            Console.WriteLine(clientId);
            var message = await _clientService.deleteClient(clientId, tripId);
            return Ok(message);
        }
    }
}