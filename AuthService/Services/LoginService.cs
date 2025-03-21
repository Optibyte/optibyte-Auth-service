using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client;
using Microsoft.Extensions.Options;
using AuthService.Models;
using AuthService.Services.Interfaces;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Exceptions;

namespace AuthService.Services
{
    public class LoginService : ILoginService, IDisposable
    {
        private readonly InfluxDBClient _influxDBClient;
        private readonly string _bucket;
        private readonly string _organization;
        private readonly ILogger<LoginService> _logger;
        private readonly IConfiguration _configuration;

        public LoginService(IOptions<InfluxDbSettings> influxDBSettings, ILogger<LoginService> logger, IConfiguration configuration)
        {
            var settings = influxDBSettings.Value;

            _influxDBClient = new InfluxDBClient(settings.Url, settings.Token);
            _bucket = settings.Bucket;
            _organization = settings.Organization;
            _logger = logger;
            _configuration = configuration;

        }

        // Get User By Email
        public async Task<Dictionary<string, object>> GetUserByEmail(string email)
        {
            try
            {
                // Step 1: Retrieve the UserId associated with the email
                var getUserIdQuery = $"from(bucket: \"{_bucket}\") " +
                                     $"|> range(start: 1970-01-01T00:00:00Z, stop: now())  " +
                                     $"|> filter(fn: (r) => r[\"_measurement\"] == \"Users\") " +
                                     $"|> filter(fn: (r) => r[\"_field\"] == \"Email\") " +
                                     $"|> filter(fn: (r) => r[\"_value\"] == \"{email}\") " +
                                     $"|> group(columns: [\"UserId\"]) " +
                                     $"|> keep(columns: [\"UserId\"])";


                var queryApi = _influxDBClient.GetQueryApi();
                var tables = await queryApi.QueryAsync(getUserIdQuery, _organization);

                if (tables == null || tables.Count == 0)
                {
                    return null;
                }

                var userId = tables[0].Records.First().GetValueByKey("UserId").ToString();

                // Step 2: Query all fields and tags for the found UserId
                var getUserDetailsQuery = $"from(bucket: \"{_bucket}\") " +
                                          $"|> range(start: 1970-01-01T00:00:00Z, stop: now()) " +
                                          $"|> filter(fn: (r) => r[\"_measurement\"] == \"Users\") " +
                                          $"|> filter(fn: (r) => r[\"UserId\"] == \"{userId}\") " +
                                          $"|> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")";


                tables = await queryApi.QueryAsync(getUserDetailsQuery, _organization);

                if (tables == null || tables.Count == 0)
                {
                    return null;
                }

                // Step 3: Map the fields and tags into a dictionary
                var userDetails = new Dictionary<string, object>();

                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        foreach (var column in table.Columns)
                        {
                            Console.WriteLine(column.Label);
                            if (column.Label != "_time" && column.Label != "_measurement" && column.Label != "_start" && column.Label != "_stop" && column.Label != "result" && column.Label != "table")
                            {
                                var value = record.GetValueByKey(column.Label);
                                userDetails[column.Label] = value ?? string.Empty;
                            }
                        }
                    }
                }


                return userDetails;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while retrieving the user from database.");
            }
        }

        // Generate JWT Token
        public string GenerateJwtToken(Dictionary<string, object> data)
        {
            try
            {

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = data
                    .Where(kvp => kvp.Key != "Password")
                    .Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()))
                    .ToList();

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Issuer"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds);

                return new JwtSecurityTokenHandler().WriteToken(token);


            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while generating token.");
            }
        }

        // Dispose Influx Client
        public void Dispose()
        {
            _influxDBClient.Dispose();
        }
    }
}
