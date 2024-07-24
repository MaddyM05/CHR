using Chorus.WindowsReader.Common.Helpers;
using Chorus.WindowsReader.Common.Logger;
using Chorus.WindowsReader.Common.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Chorus.WindowsReader.Common.Services
{
    public class LocationService
    {
        private Geolocator geolocator;
        private readonly IChorusLogger<LocationService> _logger;
        public LocationService(IChorusLogger<LocationService> logger)
        {
            _logger = logger;
            geolocator = new Geolocator();
        }

        /// <summary>
        /// Asynchronously retrieves the geographical coordinates of the device.
        /// Attempts to get coordinates from the location service first, then falls back to IP-based coordinates if necessary.
        /// Throws an exception if the location cannot be determined.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the location data.</returns>
        public async Task<LocationData> GetCoordinatesAsync()
        {
            var location = await GetCoordinatesFromLocationServiceAsync();
            if (location == null)
            {
                location = await GetCoordinatesFromIpAsync();
            }

            if (location == null)
            {
                _logger.LogException(ChorusEventType.WARNING, new ChorusCustomException("Location service requested"));
                throw new Exception("Location not available. Please turn on Location services or Wi-Fi.");
            }

            return location;
        }

        /// <summary>
        /// Asynchronously retrieves the geographical coordinates of the device using the location service.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the location data, or null if access is denied.</returns>
        private async Task<LocationData> GetCoordinatesFromLocationServiceAsync()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                try
                {
                    var geoposition = await new Geolocator().GetGeopositionAsync();
                    var locationData = new LocationData()
                    {
                        Latitude = Math.Round(geoposition.Coordinate.Point.Position.Latitude, 5),
                        Longitude = Math.Round(geoposition.Coordinate.Point.Position.Longitude, 5),
                        Source = LocationSourceConstant.WindowsLocationService
                    };
                    return locationData;
                }
                catch (Exception ex)
                {
                    string msg = $"Error getting location from GPS: {ex.Message}";
                    _logger.LogException(ChorusEventType.ERROR, new ChorusCustomException(msg, ex));
                    throw new Exception(msg);
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the geographical coordinates of the device using an IP-based service.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the location data.</returns>
        private async Task<LocationData> GetCoordinatesFromIpAsync()
        {
            string apiUrl = "http://ip-api.com/json/";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject json = JObject.Parse(responseBody);
                    var locationData = new LocationData()
                    {
                        Latitude = Convert.ToDouble(json["lat"]),
                        Longitude = Convert.ToDouble(json["Lon"]),
                        Source = LocationSourceConstant.IPBasedService
                    };

                    return locationData;
                }
                catch (HttpRequestException e)
                {
                    var msg = $"Request error: {e.Message}";
                    Debug.WriteLine(msg);
                    _logger.LogException(ChorusEventType.ERROR, new ChorusCustomException(msg, e));
                    throw new Exception(msg);
                }
                catch (Exception e)
                {
                    var msg = $"Error: {e.Message}";
                    _logger.LogException(ChorusEventType.ERROR, new ChorusCustomException(msg, e));
                    Debug.WriteLine(msg);
                    throw new Exception(msg);
                }

            }
        }
    }
}
