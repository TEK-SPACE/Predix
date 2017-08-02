using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Interface
{
    public interface IImage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageAssetUid"></param>
        /// <returns>Base64 Image</returns>
        string MediaOnDemand(string imageAssetUid, string timestamp);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        void SaveImage(Image image);
    }
}
