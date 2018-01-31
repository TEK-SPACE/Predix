using System.Collections.Generic;
using System.Threading.Tasks;

namespace Predix.Pipeline.Interface
{
    public interface IPredixHttpClient
    {
        Task<string> GetAllAsync(string url, Dictionary<string, string> additionalHeaders);
        Task<string> GetFile(string url, Dictionary<string, string> additionalHeaders);
    }
}
