using CW_7_s31270.Models;

namespace CW_7_s31270.Services
{
    public interface IDbService
    {
        Task<IEnumerable<Trip>> GetTripsAsync();
        Task<IEnumerable<object>> GetClientTripsAsync(int clientId);
        Task<bool> ClientExistsAsync(int clientId);
        Task<bool> TripExistsAsync(int tripId);
        Task<bool> IsClientRegisteredForTripAsync(int clientId, int tripId);
        Task<(int MaxPeople, int CurrentParticipants)> GetTripCapacityInfoAsync(int tripId);
        Task<int> CreateClientAsync(Client client);
        Task RegisterClientForTripAsync(int clientId, int tripId);
        Task UnregisterClientFromTripAsync(int clientId, int tripId);
    }
    
}

