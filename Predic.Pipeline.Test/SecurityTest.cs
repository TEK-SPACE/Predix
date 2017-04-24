using System;
using System.Configuration;
using System.Text;
using NUnit.Framework;
using Predic.Pipeline.Interface;
using Predic.Pipeline.Service;
using Predix.Domain.Model.Constant;

namespace Predic.Pipeline.Test
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
