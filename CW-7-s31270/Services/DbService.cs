using System.Data;
using System.Data.SqlClient;
using CW_7_s31270.Models;
using CW_7_s31270.Services;

namespace CW_7_s31270.Services;

public class DbService : IDbService
    {
        private readonly SqlConnectionStringBuilder _connectionBuilder;

        public DbService(SqlConnectionStringBuilder connectionBuilder)
        {
            _connectionBuilder = connectionBuilder;
        }
        
        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionBuilder.ConnectionString);
        }

        public async Task<IEnumerable<Trip>> GetTripsAsync()
        {
            var trips = new List<Trip>();

            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();
                
                using (var command = new SqlCommand(
                    "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople " +
                    "FROM Trip t " +
                    "ORDER BY t.DateFrom", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trips.Add(new Trip
                            {
                                IdTrip = (int)reader["IdTrip"],
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"]?.ToString(),
                                DateFrom = (DateTime)reader["DateFrom"],
                                DateTo = (DateTime)reader["DateTo"],
                                MaxPeople = (int)reader["MaxPeople"]
                            });
                        }
                    }
                }
                
                foreach (var trip in trips)
                {
                    using (var command = new SqlCommand(
                        "SELECT c.Name " +
                        "FROM Country c " +
                        "JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry " +
                        "WHERE ct.IdTrip = @IdTrip", connection))
                    {
                        command.Parameters.AddWithValue("@IdTrip", trip.IdTrip);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                trip.Countries.Add(reader["Name"].ToString());
                            }
                        }
                    }
                }
            }

            return trips;
        }

        public async Task<IEnumerable<object>> GetClientTripsAsync(int clientId)
        {
            var trips = new List<object>();

            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, " +
                    "ct.RegisteredAt, ct.PaymentDate " +
                    "FROM Trip t " +
                    "JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip " +
                    "WHERE ct.IdClient = @IdClient " +
                    "ORDER BY t.DateFrom", connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trips.Add(new
                            {
                                IdTrip = (int)reader["IdTrip"],
                                Name = reader["Name"].ToString(),
                                Description = reader["Description"]?.ToString(),
                                DateFrom = ((DateTime)reader["DateFrom"]).ToString("yyyy-MM-dd"),
                                DateTo = ((DateTime)reader["DateTo"]).ToString("yyyy-MM-dd"),
                                RegisteredAt = reader["RegisteredAt"].ToString(),
                                PaymentDate = reader["PaymentDate"] == DBNull.Value
                                    ? null
                                    : reader["PaymentDate"].ToString()
                            });
                        }
                    }
                }
            }

            return trips;
        }

        public async Task<bool> ClientExistsAsync(int clientId)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT COUNT(1) FROM Client WHERE IdClient = @IdClient", connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    return (int)await command.ExecuteScalarAsync() > 0;
                }
            }
        }

        public async Task<bool> TripExistsAsync(int tripId)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT COUNT(1) FROM Trip WHERE IdTrip = @IdTrip", connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    return (int)await command.ExecuteScalarAsync() > 0;
                }
            }
        }

        public async Task<bool> IsClientRegisteredForTripAsync(int clientId, int tripId)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                    connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    return (int)await command.ExecuteScalarAsync() > 0;
                }
            }
        }

        public async Task<(int MaxPeople, int CurrentParticipants)> GetTripCapacityInfoAsync(int tripId)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT t.MaxPeople, COUNT(ct.IdClient) AS CurrentParticipants " +
                    "FROM Trip t " +
                    "LEFT JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip " +
                    "WHERE t.IdTrip = @IdTrip " +
                    "GROUP BY t.MaxPeople", connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", tripId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var maxPeople = (int)reader["MaxPeople"];
                            var currentParticipants = reader["CurrentParticipants"] == DBNull.Value
                                ? 0
                                : (int)reader["CurrentParticipants"];

                            return (maxPeople, currentParticipants);
                        }
                        return (0, 0);
                    }
                }
            }
        }

        public async Task<int> CreateClientAsync(Client client)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) " +
                    "VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); " +
                    "SELECT SCOPE_IDENTITY();", connection))
                {
                    command.Parameters.AddWithValue("@FirstName", client.FirstName);
                    command.Parameters.AddWithValue("@LastName", client.LastName);
                    command.Parameters.AddWithValue("@Email", client.Email);
                    command.Parameters.AddWithValue("@Telephone", client.Telephone ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Pesel", client.Pesel ?? (object)DBNull.Value);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public async Task RegisterClientForTripAsync(int clientId, int tripId)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                var today = DateTime.Now.ToString("yyyyMMdd");
                using (var command = new SqlCommand(
                    "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) " +
                    "VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)", connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);
                    command.Parameters.AddWithValue("@RegisteredAt", int.Parse(today));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UnregisterClientFromTripAsync(int clientId, int tripId)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip",
                    connection))
                {
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdTrip", tripId);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }