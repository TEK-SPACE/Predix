using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using Predix.Domain.Model;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;

namespace Predix.Pipeline.Test
{
    class LocationTest
    {
        private ILocation _locationService;
        private static Dictionary<string, object> _globalVariables;

        [SetUp]
        public void Init()
        {
            _locationService = new LocationService(_globalVariables);
        }

        [Test]
        [TestCase("32.715675:-117.161230,32.708498:-117.151681", "PARKING_ZONE", 5)]
        public void AllTest(string bbox, string locationType, int size)
        {
            int totalElements = 29;
            var results = _locationService.All(bbox, locationType, size);
            Assert.AreEqual(totalElements, results.Count);
        }

        private static bool IsPointInPolygon4(PointF[] polygon, PointF testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y ||
                    polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) *
                        (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }

                j = i;
            }

            return result;
        }

        [Test]
        [TestCase(
            "33.74934619479251:-84.3899619147613,33.74938691412448:-84.39003438754975,33.749364316859086:-84.39005275218324,33.749323597527116:-84.38998027939479")]
        public void ViolationPercentage(string lotLongStr)
        {
            var matchRate = 0;
            List<string> latLongs = lotLongStr.Split(',').ToList();
            NodeMasterRegulation regulation = new NodeMasterRegulation
            {
                ParkingRegulation = new ParkingRegulation
                {
                    Coodrinate1 = "33.7492714148097:-84.3899301837381",
                    Coodrinate2 = "33.7492951970509:-84.3899094961527",
                    Coodrinate3 = "33.7493377178119:-84.3899959523559",
                    Coodrinate4 = "33.7493208925935:-84.3900166907771"
                }
            };
            foreach (var latLong in latLongs)
            {
                if (IsPointInPolygon4(new List<PointF>
                    {
                        new PointF(
                            float.Parse(regulation.ParkingRegulation.Coodrinate1.Split(':')[0]),
                            float.Parse(regulation.ParkingRegulation.Coodrinate1.Split(':')[1])),
                        new PointF(
                            float.Parse(regulation.ParkingRegulation.Coodrinate2.Split(':')[0]),
                            float.Parse(regulation.ParkingRegulation.Coodrinate2.Split(':')[1])),
                        new PointF(
                            float.Parse(regulation.ParkingRegulation.Coodrinate3.Split(':')[0]),
                            float.Parse(regulation.ParkingRegulation.Coodrinate3.Split(':')[1])),
                        new PointF(
                            float.Parse(regulation.ParkingRegulation.Coodrinate4.Split(':')[0]),
                            float.Parse(regulation.ParkingRegulation.Coodrinate4.Split(':')[1]))
                    }.ToArray(),
                    new PointF(float.Parse(latLong.Split(':')[0]),
                        float.Parse(latLong.Split(':')[1]))))
                {
                    matchRate += 25;
                }

            }

            Assert.Greater(matchRate, 0);
        }
    }
}