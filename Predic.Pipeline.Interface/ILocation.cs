using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Interface
{
    public interface ILocation
    {
        /// <summary>
        /// This API provides a list of sensor locations within a bounding box (bbox) that you define with GPS coordinates.
        /// </summary>
        /// <param name="bbox">The bounded area for your search; establishes the periphery of a searchable area for assets and nodes, identified by GPS coordinates.</param>
        /// <param name="locationType">enumeration codes for locationType <example>PARKING_SPOT consists of demarcated parking spaces within the defined boundaries (Not applicable in v2).</example><example>PARKING_ZONE consists of four geo-coordinates ( see coordinateType: GEO) indicating non-demarcated parking spaces within the defined boundaries.</example></param>
        /// <param name="size">Maximum number of records to return per page; if none specified, the default is used automatically.</param>
        /// <returns></returns>
        List<Location> All(string bbox, string locationType, int size);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationUids"></param>
        /// <returns></returns>
        List<LocationDetails> Details(List<string> locationUids);

        void SaveLocationKeys(List<Location> locationKeys);
        void SaveLocationDetails(List<LocationDetails> locationDetailsList);
    }
}
