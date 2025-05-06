using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Data.SqlClient;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientService : IClientService
{
    private readonly string _connectionString =
        "Data Source=localhost, 1433; User=SA; Password=yourStrong()Password; Initial Catalog=apbd; Integrated Security=False;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False";

    public async Task<List<ClientTripDTO>> GetClientsTrips(int id)
    {
        var trips = new Dictionary<int, ClientTripDTO>();

        string command =
            "SELECT ct.RegisteredAt, ct.PaymentDate, t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName " +
            "FROM Client_Trip ct " +
            "JOIN Trip t ON ct.IdTrip = t.IdTrip " +
            "JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip " +
            "JOIN Country c ON ctr.IdCountry = c.IdCountry " +
            "WHERE ct.IdClient = @id";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var ordinaryID = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    if (!trips.ContainsKey(ordinaryID))
                    {
                        trips[ordinaryID] = new ClientTripDTO()
                        {
                            TripName = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = new List<string>(),
                            RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                            PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("PaymentDate"))
                        };
                    }

                    trips[ordinaryID].Countries.Add(reader.GetString(reader.GetOrdinal("CountryName")));
                }
            }
        }

        return trips.Values.ToList();
    }

    public async Task<int> CreateNewClient(ClientDTO clientDto)
    {
        int newClientId;

        string command = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                           OUTPUT INSERTED.IdClient
                           VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
            cmd.Parameters.AddWithValue("@LastName", clientDto.LastName);
            cmd.Parameters.AddWithValue("@Email", clientDto.Email);
            cmd.Parameters.AddWithValue("@Telephone", clientDto.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

            await conn.OpenAsync();
            newClientId = (int)await cmd.ExecuteScalarAsync();
        }

        return newClientId;
    }

    public async Task<string> assignClientToTrip(int clientId, int tripId)
    {

        string client_sql = "select COUNT(1) from Client where IdClient = @clientId";
        string trip_sql = "select MaxPeople, (select COUNT(*) from Trip where IdTrip = @tripId) as TripExist, (select count(*) from Client_Trip where IdTrip = @tripID) as PeopleInTrip from Trip where IdTrip = @tripId";

        var peopleInTrip = 0;
        var maxPeople = 0;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(client_sql, conn))
        {
            cmd.Parameters.AddWithValue("@clientId", clientId);

            await conn.OpenAsync();
            var clientExist = (int)await cmd.ExecuteScalarAsync() > 0;
            if (!clientExist)
            {
                return $"Client with ID #{clientId} does not exist.";
            }
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(trip_sql, conn))
        {
            cmd.Parameters.AddWithValue("@tripId", tripId);

            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var tripExist = reader.GetInt32(reader.GetOrdinal("TripExist"));
                    maxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople"));
                    peopleInTrip = reader.GetInt32(reader.GetOrdinal("PeopleInTrip"));
                    if (tripExist <= 0)
                    {
                        return $"Trip with ID #{tripId} does not exist.";
                    }
                }
                else
                {
                    return $"Trip with ID #{tripId} not found.";
                }
            }
        }

        if (peopleInTrip++ <= maxPeople)
        {
            string insertCmd = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)" +
                               "VALUES (@clientId, @tripId, @registeredAt)";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(insertCmd, conn))
            {
                cmd.Parameters.AddWithValue("@clientId", clientId);
                cmd.Parameters.AddWithValue("@tripId", tripId);
                cmd.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));

                await conn.OpenAsync();
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex) when (ex.Number == 2627) // 2627 = primary key violation
                {
                    return $"Client with ID #{clientId} is already on trip with ID #{tripId}.";
                }
            }

            return $"Client with ID #{clientId} has been assigned to trip with ID #{tripId}";

        }

        return $"Trip with ID #{tripId} is full";

    }

    public async Task<string> deleteClient(int clientId, int tripId)
    {
        string checkSql = "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId";
        string deleteSql = "DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
        {
            checkCmd.Parameters.AddWithValue("@clientId", clientId);
            checkCmd.Parameters.AddWithValue("@tripId", tripId);

            await conn.OpenAsync();
            var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;
            if (!exists)
            {
                return $"No registration found for client ID #{clientId} on trip ID #{tripId}.";
            }

            using (SqlCommand deleteCmd = new SqlCommand(deleteSql, conn))
            {
                deleteCmd.Parameters.AddWithValue("@clientId", clientId);
                deleteCmd.Parameters.AddWithValue("@tripId", tripId);

                await deleteCmd.ExecuteNonQueryAsync();
                return $"Client ID #{clientId} successfully removed from trip ID #{tripId}.";
            }
        }
    }
}