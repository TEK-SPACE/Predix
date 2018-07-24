using System;
using System.Collections.Generic;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predix.Pipeline.Interface
{
    public interface IImage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parkingEvent"></param>
        /// <param name="imageAssetUid"></param>
        /// <param name="timestamp"></param>
        /// <param name="customer"></param>
        /// <returns>Base64 Image</returns>
        void MediaOnDemand(ParkingEvent parkingEvent, string imageAssetUid, string timestamp, Customer customer);

        Image MarkPixelCoordinates(ParkingEvent parkingEvent, Image image, Customer customer);
        List<Tuple<int, string, string>> GetRecentBase64();
    }
}
