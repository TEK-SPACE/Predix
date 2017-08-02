using NUnit.Framework;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;

namespace Predic.Pipeline.Test
{
    class LocationTest
    {
        private ILocation _locationService;

        [SetUp]
        public void Init()
        {
            _locationService = new LocationService();
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