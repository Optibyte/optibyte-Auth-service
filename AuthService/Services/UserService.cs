using InfluxDB.Client;
using Microsoft.Extensions.Options;
using AuthService.Models;
using AuthService.Services.Interfaces;
using AuthService.Utilities;
using NodaTime;
using AuthService.Exceptions;


namespace AuthService.Services
{
    public class UserService : IUserService
    {
        private readonly InfluxDBClient _influxDBClient;
        private readonly string _bucket;
        private readonly string _organization;
        private readonly ILogger<UserService> _logger;

        public UserService(IOptions<InfluxDbSettings> influxDBSettings, ILogger<UserService> logger)
        {
            var settings = influxDBSettings.Value;
            _influxDBClient = new InfluxDBClient(settings.Url, settings.Token);
            _bucket = settings.Bucket;
            _organization = settings.Organization;
            _logger = logger;
        }

        // Creates users in InfluxDB
        public async Task<bool> CreateUser(UserDataRequest user, bool isUpdate)
        {
            try
            {
                if (user == null)
                    throw new UserServiceException("User cannot be null!");

                // Check if the new email already exists
                var emailExists = await CheckIfEmailExists(user.Email);
                if (emailExists)
                {
                    throw new Exception("Email already exists!");
                }

                var userDataResponse = new UserDataResponse
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Type = user.Type,
                    Company = user.Company,
                    Site = user.Site,
                    Zone = user.Zone,
                    Role = user.Role,
                    Email = user.Email,
                    Password = user.Password,
                    serviceType = user.serviceType,

                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };


                var companyDetails = await GetCompanyDetails(user.Company);
                if (companyDetails == null)
                {
                    throw new Exception("Company not found!");
                }
                Console.WriteLine(companyDetails);
                Console.WriteLine(companyDetails.Topic);

                userDataResponse.Bucket = companyDetails.Topic;
                userDataResponse.CompanyId = companyDetails.Id;


                if (!string.IsNullOrEmpty(user.Zone))
                {
                    var zoneDetails = await GetZoneDetails(user.Zone);
                    if (zoneDetails != null)
                    {
                        userDataResponse.ZoneId = zoneDetails.Id;
                    }
                }


                if (!string.IsNullOrEmpty(user.Site))
                {
                    var siteDetails = await GetSiteDetails(user.Site);
                    if (siteDetails != null)
                    {
                        userDataResponse.SiteId = siteDetails.Id;
                    }
                }

                var point = UserPointBuilder.Build(userDataResponse, isUpdate);
                var writeApi = _influxDBClient.GetWriteApiAsync();

                await writeApi.WritePointAsync(point, _bucket, _organization);

                return true;
            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while creating the user in database.");
            }
        }


        // Get all users from InfluxDB
        public async Task<List<UserDataResponse>> GetAllUsers(string type,int pageNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                    throw new UserServiceException("User type must be provided!");
                int pageSize = 50;
                int offset = (pageNumber - 1) * pageSize;

                offset = (offset * 13);
                var fluxQuery = $@"
                 from(bucket: ""{_bucket}"") 
                 |> range(start: 0) 
                 |> filter(fn: (r) => r._measurement == ""Users"" and r[""Type""] == ""{type}"")
                 |> group()
                 |> sort(columns: [""UserId""], desc: true) 
                 |> limit(n: {13 * pageSize}, offset: {offset})
                 ";
                var fluxTables = await _influxDBClient.GetQueryApi().QueryAsync(fluxQuery, _organization);

                // Parsing the table
                var users = UserParser.ParseUsers(fluxTables);
                return users;
            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while retrieving all users from database.");
            }
        }

        // Get user by UserId in InfluxDB
        public async Task<UserDataResponse> GetUserById(Guid userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId.ToString()))
                    throw new UserServiceException("UserId must be provided!");

                var fluxQuery = $"from(bucket: \"{_bucket}\") |> range(start: 0) |> filter(fn: (r) => r[\"UserId\"] == \"{userId}\")";
                var fluxTables = await _influxDBClient.GetQueryApi().QueryAsync(fluxQuery, _organization);

                // Parse the table
                var user = UserParser.ParseUser(fluxTables, userId);
                if (user == null)
                {
                    return null;
                }

                return user;
            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while retrieving the user from database.");
            }
        }

        // Update a user in InfluxDB
        public async Task<bool> UpdateUser(UserDataRequest user, Guid userId, HttpContext httpContext)
        {
            try
            {
                if (user == null)
                    throw new UserServiceException("User cannot be null!");

                // Get the current user from the database by their userId
                var currentUser = await GetUserById(userId);
                if (currentUser == null)
                {
                    throw new UserServiceException("User not found!");
                }

                 // Check if the email has changed
                if (!string.Equals(currentUser.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    // If email has changed, check if the new email already exists
                    var emailExists = await CheckIfEmailExists(user.Email);
                    if (emailExists)
                    {
                        throw new Exception("Email already exists!");
                    }
                }


                await DeleteUser(userId, httpContext);

                var result = await CreateUser(user, true);

                if (result)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while updating the user.");
            }
        }

        // Delete user in InfluxDB
        public async Task<bool> DeleteUser(Guid userId, HttpContext httpContext)
        {
            try
            {


                if (string.IsNullOrEmpty(userId.ToString()))
                    throw new UserServiceException("UserId must be provided!");

                var user = await GetUserById(userId);

                if (user == null)
                {
                    throw new UserServiceException("UserId not found! No deletion performed.");
                }

                if (!AuthorizationHelper.AuthorizeUser(httpContext, user.Type, out var errorMessage))
                {
                    throw new UserServiceException(errorMessage);
                }



                var deleteApi = _influxDBClient.GetDeleteApi();
                if (deleteApi == null)
                    throw new UserServiceException("Delete API is not initialized!");

                var predicate = $"_measurement=\"Users\" AND UserId=\"{userId}\"";
                var start = DateTime.UtcNow.AddYears(-1);
                var stop = DateTime.UtcNow;

                await deleteApi.Delete(start, stop, predicate, _bucket, _organization);
                _logger.LogInformation($"User with UserId: {userId} deleted successfully.");
                return true;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while deleting the user from database.");
            }
        }

        public async Task<bool> CheckIfEmailExists(string email)
        {
            try
            {

                var query = $@"
                    from(bucket: ""{_bucket}"")
                    |> range(start: 1970-01-01T00:00:00Z, stop: now())
                    |> filter(fn: (r) => r._measurement == ""Users"")
                    |> filter(fn: (r) => r._field == ""Email"" and r._value == ""{email}"")";

                var tables = await _influxDBClient.GetQueryApi().QueryAsync(query, _organization);

                if (tables.Any())
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("Error checking email existence.");
            }
        }

        public async Task<CompanyData> GetCompanyDetails(string companyId)
        {
            try
            {
                string fluxQuery = $"from(bucket: \"{_bucket}\") " +
                                   $"|> range(start: 0) " +
                                   $"|> filter(fn: (r) => r._measurement == \"Companies\" and r.CompanyId == \"{companyId}\")";

                var fluxTables = await _influxDBClient.GetQueryApi().QueryAsync(fluxQuery, _organization);
                Console.WriteLine(fluxTables);
                var companyData = UserParser.ParseCompanyData(fluxTables, companyId);

                if (companyData == null)
                {
                    throw new Exception("Company details not found!");
                }

                return companyData;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occured while retrieving company details!");
            }
        }

        public async Task<ZoneData> GetZoneDetails(string zoneId)
        {
            try
            {
                string fluxQuery = $"from(bucket: \"{_bucket}\") " +
                                   $"|> range(start: 0) " +
                                   $"|> filter(fn: (r) => r._measurement == \"Zones\" and r.ZoneId == \"{zoneId}\")";

                var fluxTables = await _influxDBClient.GetQueryApi().QueryAsync(fluxQuery, _organization);

                var zoneData = UserParser.ParseZoneData(fluxTables, zoneId);

                if (zoneData == null)
                {
                    throw new Exception("Zone details not found!");
                }

                return zoneData;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occured while retrieving zone details!");
            }
        }

        public async Task<SiteData> GetSiteDetails(string siteId)
        {
            try
            {
                string fluxQuery = $"from(bucket: \"{_bucket}\") " +
                                   $"|> range(start: 0) " +
                                   $"|> filter(fn: (r) => r._measurement == \"Sites\" and r.SiteId == \"{siteId}\")";

                var fluxTables = await _influxDBClient.GetQueryApi().QueryAsync(fluxQuery, _organization);

                var siteData = UserParser.ParseSiteData(fluxTables, siteId);

                if (siteData == null)
                {
                    throw new Exception("Site details not found!");
                }

                return siteData;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occured while retrieving site details!");
            }
        }



    }
}

