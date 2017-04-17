using System.Collections.Generic;
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
        /// <param name="page">Indicates the page number; default value is 0</param>
        /// <returns></returns>
        List<Identifier> All(string bbox, string locationType, int size, int page = 0);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationUid">The identifier assigned to this location<example>LOCATION-282</example></param>
        /// <param name="eventType">Filter for pedestrian events<example>["PKIN,PKOUT"]</example></param>
        /// <returns></returns>
        Details Get(string locationUid, string eventType);

        void SaveLocationKeys(List<Identifier> locationKeys);
        void SaveLocationDetails(List<Details> locationDetails);
    }
}
