using System.Collections.Generic;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predix.Pipeline.Interface
{
    public interface IBatch : ILocation
    {
        Activity PullAndSaveAllLocations();
        Activity PullAndSaveLocationDetails(List<Location> identifiers);
    }
}
