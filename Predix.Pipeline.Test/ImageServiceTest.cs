using System.Collections.Generic;
using NUnit.Framework;
using Predix.Domain.Model.Location;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;

namespace Predix.Pipeline.Test
{
    public class ImageServiceTest
    {
        private IImage _imageService;
        private static Dictionary<string, object> _globalVariables;
        [SetUp]
        public void Init()
        {
            _globalVariables = new Dictionary<string, object>();
            _imageService = new ImageService(_globalVariables);
        }
        [Test]
        [TestCase(1,"ba8fc508-e362-43f3-8149-9605459c0896", "1512071220607")]
        public void MediaOnDemandTest(int propertyId, string imageAssetUid, string timestamp)
        {

            _imageService.MediaOnDemand(new ParkingEvent {Properties = new ParkingEventProperties {Id = propertyId}},
                imageAssetUid, timestamp);
        }
    }
}