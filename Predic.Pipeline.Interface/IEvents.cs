using System;
using System.Collections.Generic;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Interface
{
    public interface  IEvents
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationUid">The identifier assigned to this location<example>LOCATION-282</example></param>
        /// <param name="eventType">Filter for pedestrian events<example>["PKIN,PKOUT"]</example></param>
        /// <returns></returns>
        ParkingEvent Get(string locationUid, string eventType);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationUid"></param>
        /// <param name="eventType"></param>
        /// <param name="startDate"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        List<ParkingEvent> Get(string locationUid, string eventType, DateTime startDate, DateTime endTime);
    }
}
