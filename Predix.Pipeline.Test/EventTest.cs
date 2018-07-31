using System;
using System.Collections.Generic;
using NUnit.Framework;
using Predix.Domain.Model;
using Predix.Domain.Model.Location;
using Predix.Pipeline.Helper;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;

namespace Predix.Pipeline.Test
{
    class EventTest
    {
        private IEvent _eventService;
        private static Dictionary<string, object> _globalVariables;
        [SetUp]
        public void Init()
        {
            _eventService = new EventService(_globalVariables);
        }
        [Test]
        [TestCase("LOCATION-225", "PKIN", 4120)]
        public void GetWebSocketTest(string locationUid, string eventType, int customerId)
        {
            _eventService.GetByLocation(locationUid, eventType, new ImageService(new Dictionary<string, object>()),
                new Options(), new Customer() {Id = customerId});
            //Assert.IsNotNull(details);
            //Assert.AreSame(new ParkingEvent(), details);
        }

        [Test]
        [TestCase("LOCATION-225", "PKIN")]
        public void GetTest(string locationUid, string eventType)
        {
            DateTime startDate = new DateTime(2015, 10, 28, 18, 58, 57, DateTimeKind.Utc);
            DateTime endDate = new DateTime(2017, 10, 28, 19, 12, 17, DateTimeKind.Utc);
            var details = _eventService.Get(locationUid, eventType, startDate.ToEpoch().ToString(), endDate.ToEpoch().ToString());
            Assert.IsNotNull(details);
            Assert.AreSame(new List<ParkingEvent>(), details);
        }
    }
}
