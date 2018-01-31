using System.Collections.Generic;
using NUnit.Framework;
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
            var results =  _locationService.All(bbox, locationType, size);
            Assert.AreEqual(totalElements, results.Count);
        }
    }
}