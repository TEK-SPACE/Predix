using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Predic.Pipeline.Interface
{
    public interface IPredixWebSocketClient
    {
        Task<string> GetAllAsync(string url, string bodyMessage, Dictionary<string, string> additionalHeaders);
    }
}
