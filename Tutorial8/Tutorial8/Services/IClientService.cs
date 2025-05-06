using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientService
{
    Task<List<ClientTripDTO>> GetClientsTrips(int id);
    Task<int> CreateNewClient(ClientDTO clientDto);
    Task<string> assignClientToTrip(int clientId, int tripid);
    Task<string> deleteClient(int clientId, int tripId);

}