using System.Collections.Generic;
using NUnit.Framework;


namespace Litle.Sdk.Test.Unit
{
    [TestFixture]
    internal class TestCommunications
    {
        private Communications _objectUnderTest;

        [OneTimeSetUp]
        public void SetUpLitle()
        {
            _objectUnderTest = new Communications();
        }

        [Test]
        public void TestSettingProxyPropertiesToNullShouldTurnOffProxy()
        {
            var config = new Dictionary<string, string> {{"proxyHost", null}, {"proxyPort", null}};

            Assert.IsFalse(_objectUnderTest.IsProxyOn(config));
        }

        [Test]
        public void TestNeuterXmlRedactsTrackData()
        {
            string xml = "<track>SENSITIVE_TRACK_DATA</track>";
            _objectUnderTest.NeuterXml(ref xml);
            Assert.AreEqual("<track>xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx</track>", xml);
        }

        [Test]
        public void TestNeuterXmlRedactsAllSensitiveFields()
        {
            string xml = "<number>4100000000000002</number><accNum>12345657890</accNum><track>TRACKDATA</track>";
            _objectUnderTest.NeuterXml(ref xml);
            Assert.That(xml, Does.Not.Contain("4100000000000002"));
            Assert.That(xml, Does.Not.Contain("12345657890"));
            Assert.That(xml, Does.Not.Contain("TRACKDATA"));
        }

        [Test]
        public void TestHttpPostThrowsOnMissingUrl()
        {
            var config = new Dictionary<string, string>();
            Assert.Throws<LitleOnlineException>(() => _objectUnderTest.HttpPost("<request/>", config));
        }

        [Test]
        public void TestHttpPostThrowsOnEmptyUrl()
        {
            var config = new Dictionary<string, string> {{"url", ""}};
            Assert.Throws<LitleOnlineException>(() => _objectUnderTest.HttpPost("<request/>", config));
        }

        [Test]
        public void TestHttpPostThrowsOnNullUrl()
        {
            var config = new Dictionary<string, string> {{"url", null}};
            Assert.Throws<LitleOnlineException>(() => _objectUnderTest.HttpPost("<request/>", config));
        }

        [Test]
        public void TestInvalidTimeoutUsesDefault()
        {
            // Non-numeric timeout should not throw during config parsing;
            // the SDK logs a warning and uses the default value of 500s.
            // The actual HTTP call will fail for other reasons (invalid URL),
            // but timeout parsing itself should be resilient.
            var config = new Dictionary<string, string>
            {
                {"url", "https://localhost:0/invalid"},
                {"timeout", "not-a-number"}
            };
            // Should throw LitleOnlineException due to connection failure,
            // NOT due to timeout parsing. This verifies timeout parsing is resilient.
            var ex = Assert.Throws<LitleOnlineException>(() => _objectUnderTest.HttpPost("<request/>", config));
            Assert.That(ex.Message, Does.Not.Contain("timeout"));
        }

        [Test]
        public void TestInvalidMaxConnectionsUsesDefault()
        {
            // Non-numeric maxConnections should not throw during config parsing;
            // the SDK logs a warning and uses the default value of 10.
            var config = new Dictionary<string, string>
            {
                {"url", "https://localhost:0/invalid"},
                {"maxConnections", "abc"}
            };
            // Should throw LitleOnlineException due to connection failure,
            // NOT due to maxConnections parsing. This verifies parsing is resilient.
            var ex = Assert.Throws<LitleOnlineException>(() => _objectUnderTest.HttpPost("<request/>", config));
            Assert.That(ex.Message, Does.Not.Contain("maxConnections"));
        }

        [Test]
        public void TestProxyOnWithEmptyHostReturnsFalse()
        {
            var config = new Dictionary<string, string> {{"proxyHost", ""}, {"proxyPort", "8080"}};
            Assert.IsFalse(_objectUnderTest.IsProxyOn(config));
        }

        [Test]
        public void TestProxyOnWithEmptyPortReturnsFalse()
        {
            var config = new Dictionary<string, string> {{"proxyHost", "proxy.example.com"}, {"proxyPort", ""}};
            Assert.IsFalse(_objectUnderTest.IsProxyOn(config));
        }

        [Test]
        public void TestProxyOnWithValidSettingsReturnsTrue()
        {
            var config = new Dictionary<string, string> {{"proxyHost", "proxy.example.com"}, {"proxyPort", "8080"}};
            Assert.IsTrue(_objectUnderTest.IsProxyOn(config));
        }

    }
}
