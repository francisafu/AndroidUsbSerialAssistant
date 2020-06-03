using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AndroidLocation
{
    public interface ILocationService
    {
        Task<Location> GetLocation();
    }
}