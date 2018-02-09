namespace Predix.Pipeline.Interface
{
    public interface IImage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="imageAssetUid"></param>
        /// <param name="timestamp"></param>
        /// <returns>Base64 Image</returns>
        void MediaOnDemand(int propertyId, string imageAssetUid, string timestamp);
    }
}
