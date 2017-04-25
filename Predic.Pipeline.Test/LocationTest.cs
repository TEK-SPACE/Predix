using System;
using System.Collections.Generic;
using NUnit.Framework;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;
using Predix.Domain.Model.Location;

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

        [Test]
        [TestCase("LOCATION-STG-189", "PKIN")]
        public void GetWebSocketTest(string locationUid, string eventType)
        {
            var details = _locationService.Get(locationUid, eventType);
            Assert.IsNotNull(details);
            Assert.AreSame(new ParkingEvent(), details);
        }

        [Test]
        [TestCase("LOCATION-STG-189", "PKIN")]
        public void GetTest(string locationUid, string eventType)
        {
            DateTime startDate = new DateTime(2015,10,28,18,58,57, DateTimeKind.Utc);
            DateTime endDate= new DateTime(2017, 10, 28, 19, 12, 17, DateTimeKind.Utc);
            var details = _locationService.Get(locationUid, eventType, startDate, endDate);
            Assert.IsNotNull(details);
            Assert.AreSame(new List<ParkingEvent>(), details);
        }
    }
}