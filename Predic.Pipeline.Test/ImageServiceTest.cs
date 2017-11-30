using System.Collections.Generic;
using NUnit.Framework;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;

namespace Predic.Pipeline.Test
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
        [TestCase("ba8fc508-e362-43f3-8149-9605459c0896", "1512071220607")]
        public void MediaOnDemandTest(string imageAssetUid, string timestamp)
        {
            var results = _imageService.MediaOnDemand(imageAssetUid, timestamp);
        }
    }
}