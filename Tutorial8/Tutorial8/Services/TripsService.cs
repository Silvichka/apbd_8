using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=localhost, 1433; User=SA; Password=yourStrong()Password; Initial Catalog=apbd; Integrated Security=False;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new Dictionary<int, TripDTO>();

        string command = 
            "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName " +
            "FROM Trip t " +
            "JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip " +
            "JOIN Country c ON ct.IdCountry = c.IdCountry;";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var ordinaryID = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    if (!trips.ContainsKey(ordinaryID))
                    {
                        trips[ordinaryID] = new TripDTO()
                        {
                            Id = ordinaryID,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = new List<CountryDTO>()
                        };
                    }
                    trips[ordinaryID].Countries.Add(new CountryDTO()
                    {
                        Name = reader.GetString(reader.GetOrdinal("CountryName"))
                    });
                }
            }
        }
        return trips.Values.ToList();
    }
    
}