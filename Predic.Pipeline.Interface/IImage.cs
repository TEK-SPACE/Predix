using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Interface
{
    public interface IImage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageAssetUid"></param>
        /// <returns></returns>
        Image MediaOnDemand(string imageAssetUid);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        void SaveImage(Image image);
    }
}
