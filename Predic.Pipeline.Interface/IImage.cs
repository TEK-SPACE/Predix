using System.Threading.Tasks;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Interface
{
    public interface IImage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageAssetUid"></param>
        /// <param name="timestamp"></param>
        /// <returns>Base64 Image</returns>
        void MediaOnDemand(string imageAssetUid, string timestamp);
    }
}
