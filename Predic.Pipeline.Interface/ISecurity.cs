using System.Threading.Tasks;

namespace Predic.Pipeline.Interface
{
    public interface ISecurity
    {
        Task<string> SetClientToken();
    }
}
