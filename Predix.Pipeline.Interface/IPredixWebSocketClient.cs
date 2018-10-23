using System.Collections.Generic;
using System.Threading.Tasks;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predix.Pipeline.Interface
{
    public interface IPredixWebSocketClient
    {
        void OpenAsync(string url, string bodyMessage, Dictionary<string, string> additionalHeaders,
            IImage imageService, Options options, Customer customer);
        bool ProcessEvent(IImage imageService, Options options, Customer customer, ParkingEvent parkingEvent);
    }
}
