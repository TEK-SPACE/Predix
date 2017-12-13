using System;
using System.Collections.Generic;
using NUnit.Framework;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;
using Predix.Domain.Model.Location;

namespace Predic.Pipeline.Test
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
        [TestCase("LOCATION-225", "PKIN")]
        public void GetWebSocketTest(string locationUid, string eventType)
        {
            _eventService.GetByLocation(locationUid, eventType,new ImageService(new Dictionary<string, object>()));
            //Assert.IsNotNull(details);
            //Assert.AreSame(new ParkingEvent(), details);
        }

        [Test]
        [TestCase("LOCATION-225", "PKIN")]
        public void GetTest(string locationUid, string eventType)
        {
            DateTime startDate = new DateTime(2015, 10, 28, 18, 58, 57, DateTimeKind.Utc);
            DateTime endDate = new DateTime(2017, 10, 28, 19, 12, 17, DateTimeKind.Utc);
            var details = _eventService.Get(locationUid, eventType, startDate, endDate);
            Assert.IsNotNull(details);
            Assert.AreSame(new List<ParkingEvent>(), details);
        }
    }
}
