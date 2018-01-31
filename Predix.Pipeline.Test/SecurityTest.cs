using NUnit.Framework;
using Predix.Domain.Model.Constant;
using Predix.Pipeline.Interface;
using Predix.Pipeline.Service;

namespace Predix.Pipeline.Test
{
    [TestFixture]
    public class SecurityTest
    {
        private ISecurity _securityService;

        [SetUp]
        public void Init()
        {
            _securityService = new SecurityService();
        }
        [Test]
        public void TokenTest()
        {
            Assert.AreEqual("Y2l2aWNzbWFydDpDMXYxY1NtYXJ0", Endpoint.OAuthToken);
        }

        [Test]
        public void ClientAccessTokenTest()
        {
            var accessToken = _securityService.SetClientToken();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(accessToken.Result));
        }
    }
}
