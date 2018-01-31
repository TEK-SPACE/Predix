namespace Predix.Pipeline.Interface
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
