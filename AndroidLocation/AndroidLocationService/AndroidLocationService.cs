using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AndroidLocation.AndroidLocationService
{
    public class AndroidLocationService : ILocationService
    {
        public async Task<Location> GetLocation()
        {
            try
            {
                // On Android: Medium: 100-500m, High: <100m, Best: <100m
                var request = new GeolocationRequest(GeolocationAccuracy.High);
                var location = await Geolocation.GetLocationAsync(request);
                return location;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }
    }
}