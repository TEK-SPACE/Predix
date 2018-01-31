using System.Threading.Tasks;

namespace Predix.Pipeline.Interface
{
    public interface ISecurity
    {
        Task<string> SetClientToken();
    }
}
