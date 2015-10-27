using System;
using System.Dynamic;
using System.IO;
using Configuratively.Api;
using Configuratively.Hosting;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Configuratively.Tests
{
    [TestFixture]
    public class ConfigurationReaderTests
    {
        private ConfigurationReader _reader;

        [TestFixtureSetUp]
        public void Setup()
        {
            var path = Path.GetFullPath(@"..\..\_testdata\scenario3");
            var mapping = Path.GetFullPath(@"..\..\_testdata\scenario3\mapping.cfg");

            _reader = new ConfigurationReader(new ConfigSettings(path, mapping));

        }

        [Test]
        public void CanResolveConfiguration()
        {
            var dev = _reader.Get("environments/dev");
            dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(dev);

            Assert.AreEqual("basevalue", jsonObject.settings.basekey);
        }
    }
}
