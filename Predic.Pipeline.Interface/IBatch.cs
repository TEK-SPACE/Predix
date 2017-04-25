using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Interface
{
    public interface IBatch : ILocation
    {
        Activity PullAndSaveAllLocations();
        Activity PullAndSaveLocationDetails(List<Location> identifiers);
    }
}
