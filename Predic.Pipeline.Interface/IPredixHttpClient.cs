using System.Collections.Generic;
using System.Threading.Tasks;

namespace Predic.Pipeline.Interface
{
    public interface IPredixHttpClient
    {
        Task<string> GetAllAsync(string url, Dictionary<string, string> additionalHeaders);
    }
}
