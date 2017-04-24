using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;

namespace Predic.Pipeline.UI
{
    static class Program
    {
        private static ILocation _locationService;
        static void Main(string[] args)
        {
            Init();

            var bbox = "32.715675:-117.161230,32.708498:-117.151681";
            var locationType = "PARKING_ZONE";
            int pagesize = 5;
            var identifiers = _locationService.All(bbox, locationType, pagesize);
            foreach (var identifier in identifiers)
            {
                _locationService.Get(identifier.Uid, "[\"PKIN,PKOUT\"]");
            }
        }

       static void Init()
        {
            _locationService = new LocationService();
        }
    }
}
