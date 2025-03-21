using InfluxDB.Client.Core.Flux.Domain;
using AuthService.Models;
using AuthService.Exceptions;
using InfluxDB.Client.Api.Domain;
using System.ComponentModel.Design;

namespace AuthService.Utilities
{
    public static class UserParser
    {
        public static List<UserDataResponse> ParseUsers(List<FluxTable> fluxTables)
        {
            try
            {
                var userDictionary = new Dictionary<string, UserDataResponse>();

                foreach (var table in fluxTables)
                {
                    foreach (var record in table.Records)
                    {
                        var idValue = record.GetValueByKey("UserId").ToString();
                        if (!Guid.TryParse(idValue, out Guid id))
                            continue;

                        if (!userDictionary.TryGetValue(idValue, out var user))
                        {
                            user = new UserDataResponse
                            {
                                UserId = id,
                            };

                            userDictionary[idValue] = user;
                        }

                        SetUserField(user, record);
                    }
                }

                return userDictionary.Values.ToList();
            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while parsing all the users.");
            }
        }

        public static UserDataResponse ParseUser(List<FluxTable> fluxTables, Guid userId)
        {
            try
            {
                var recordsById = fluxTables
                    .SelectMany(table => table.Records)
                    .Where(record => record.GetValueByKey("UserId") != null)
                    .GroupBy(record => record.GetValueByKey("UserId")?.ToString())
                    .ToDictionary(group => group.Key, group => group.ToList());

                if (recordsById.TryGetValue(userId.ToString(), out var records))
                {
                    var user = new UserDataResponse
                    {
                        UserId = userId,
                    };
                    PopulateUserFields(user, records);
                    return user;
                }

                return null;

            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while parsing the user.");
            }

        }

        private static void PopulateUserFields(UserDataResponse user, IEnumerable<FluxRecord> records)
        {
            try
            {
                foreach (var record in records)
                {
                    SetUserField(user, record);
                }
            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while populating user fields.");
            }
        }

        private static void SetUserField(UserDataResponse user, FluxRecord record)
        {
            try
            {

                var value = record.GetValue()?.ToString() ?? string.Empty;
                var type = record.Values.ContainsKey("Type") ? record.Values["Type"]?.ToString() : null;

                if (type != null)
                {
                    user.Type = type;
                }
                Console.WriteLine($"Extracted Type: {type}");
                switch (record.GetField())
                {
                    case "Name":
                        user.Name = value;
                        break;
                    case "Email":
                        user.Email = value;
                        break;
                    case "Password":
                        user.Password = value;
                        break;
                    case "serviceType":
                        user.serviceType = value;
                        break;
                    case "CreatedAt":
                        user.CreatedAt = ParseDate(value);
                        break;
                    case "UpdatedAt":
                        user.UpdatedAt = ParseDate(value);
                        break;
                    case "Company":
                        user.Company = value;
                        break;
                    case "Bucket":
                        user.Bucket = value;
                        break;
                    case "CompanyId":
                        user.CompanyId = value;
                        break;
                    case "Role":
                        user.Role = value;
                        break;
                    case "Zone":
                        user.Zone = value;
                        break;
                    case "ZoneId":
                        user.ZoneId = value;
                        break;
                    case "Site":
                        user.Site = value;
                        break;
                    case "SiteId":
                        user.SiteId = value;
                        break;
                }
            }
            catch (UserServiceException ex)
            {
                throw new UserServiceException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while setting user fields");
            }

        }


        private static string GetFieldValue(IEnumerable<FluxRecord> records, string fieldName)
            => records.FirstOrDefault(record => record.GetValueByKey("_field")?.ToString() == fieldName)?
               .GetValueByKey("_value")?.ToString()!;

        private static DateTime ParseDate(string dateString)
        {
            try
            {
                if (string.IsNullOrEmpty(dateString))
                    return DateTime.MinValue;

                if (DateTime.TryParse(dateString, out DateTime parsedDate))
                    return parsedDate;

                Console.WriteLine($"Failed to parse date: {dateString}");
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                throw new UserServiceException("An error occurred while parsing date string");
            }
        }

        public static CompanyData ParseCompanyData(List<FluxTable> fluxTables, string companyId)
        {
            var recordsById = fluxTables
                .SelectMany(table => table.Records)
                .Where(record => record.GetValueByKey("CompanyId") != null)
                .GroupBy(record => record.GetValueByKey("CompanyId")?.ToString())
                .ToDictionary(group => group.Key, group => group.ToList());

            if (recordsById.TryGetValue(companyId, out var records))
            {
                var company = new CompanyData();

                foreach (var record in records)
                {
                    var value = record.GetValue()?.ToString() ?? string.Empty;

                    switch (record.GetField())
                    {
                        case "ShortId":
                            company.Id = value;
                            break;
                        case "Topic":
                            company.Topic = value;
                            break;
                    }

                }

                return company;
            }

            return null;
        }


        public static ZoneData ParseZoneData(List<FluxTable> fluxTables, string zoneId)
        {
            var recordsById = fluxTables
                .SelectMany(table => table.Records)
                .Where(record => record.GetValueByKey("ZoneId") != null)
                .GroupBy(record => record.GetValueByKey("ZoneId")?.ToString())
                .ToDictionary(group => group.Key, group => group.ToList());

            if (recordsById.TryGetValue(zoneId, out var records))
            {
                var zone = new ZoneData();

                foreach (var record in records)
                {
                    var value = record.GetValue()?.ToString() ?? string.Empty;

                    switch (record.GetField())
                    {
                        case "ShortId":
                            zone.Id = value;
                            break;
                    }
                }

                return zone;
            }

            return null;
        }

        public static SiteData ParseSiteData(List<FluxTable> fluxTables, string siteId)
        {
            var recordsById = fluxTables
                .SelectMany(table => table.Records)
                .Where(record => record.GetValueByKey("SiteId") != null)
                .GroupBy(record => record.GetValueByKey("SiteId")?.ToString())
                .ToDictionary(group => group.Key, group => group.ToList());

            if (recordsById.TryGetValue(siteId, out var records))
            {
                var site = new SiteData();

                foreach (var record in records)
                {
                    var value = record.GetValue()?.ToString() ?? string.Empty;

                    switch (record.GetField())
                    {
                        case "ShortId":
                            site.Id = value;
                            break;
                    }
                }

                return site;
            }

            return null;
        }




    }
}
