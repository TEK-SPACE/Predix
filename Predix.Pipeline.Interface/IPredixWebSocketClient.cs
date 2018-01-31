using System.Collections.Generic;
using System.Threading.Tasks;
using Predix.Domain.Model;

namespace Predix.Pipeline.Interface
{
    public interface IPredixWebSocketClient
    {
        Task OpenAsync(string url, string bodyMessage, Dictionary<string, string> additionalHeaders,
            IImage imageService, Options options);
    }
}
