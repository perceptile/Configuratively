using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dynamitey;
using NUnit.Framework;

using Configuratively.Domain;

namespace Configuratively.Tests
{
    [TestFixture]
    public class DynamicRepositoryTests
    {
        private DynamicRepository _repo;

        [TestFixtureSetUp]
        public void Setup()
        {
            var repoPath = Path.GetFullPath(@"..\..\_testdata\set1");

            var jsonFiles = Directory.GetFiles(repoPath, "*.json", SearchOption.AllDirectories);

            _repo = new DynamicRepository(repoPath);
            var jsonDocuments = Helpers.ReadAllJsonFiles(jsonFiles, repoPath, "");
            _repo.ProcessAllLinks(jsonDocuments);
        }

        [TestCase]
        public void SingleDepthLinksShouldBeResolve()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.IsNotNull(item.environmentType);
            Assert.AreEqual("test", item.environmentType);
        }

        [TestCase]
        public void NestedLinksShouldBeResolve()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.IsNotNull(item.networks);
            Assert.AreEqual("Admin", item.networks[0].name);
        }

        [TestCase]
        public void LocalValuesShouldOverrideLinkValues()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.IsNotNull(item.msDeployPort);
            Assert.AreEqual("8080", item.msDeployPort);
        }

        [TestCase]
        public void ComplexCollectionTypesThatExistInLinksAndLocallyShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.IsNotNull(item.networks);
            Assert.AreEqual(2, item.networks.Length);
            Assert.AreEqual("direct", item.networks[0].type);
            Assert.AreEqual("false", item.networks[0].disabled);
        }

        [TestCase]
        public void OverlappingComplexCollectionTypesThatExistInLinksAndLocallyShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/test01.json");

            Assert.IsNotNull(item.networks);
            Assert.AreEqual(2, item.networks.Length);
            Assert.AreEqual("direct", item.networks[1].type);
            Assert.AreEqual("true", item.networks[1].disabled);
        }

        [TestCase]
        public void OverlappingComplexCollectionTypesAcrossMultipleTemplatesShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.IsNotNull(item.networks);
            Assert.AreEqual(2, item.networks.Length);
            Assert.AreEqual("routed", item.networks[1].type);
        }

        [TestCase]
        public void ComplexTypesThatExistInLinksAndLocallyShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.AreEqual("bar", item.settings.foo);
            Assert.AreEqual("foo", item.settings.bar);
        }

        [TestCase]
        public void NestedArraysWithinComplexTypesShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "environments/dev01.json");

            Assert.AreEqual("notepad", item.applications[0].name);
            Assert.AreEqual("overridden", Dynamic.InvokeGet(item.applications[0].settings, "a"));
            Assert.AreEqual("3", Dynamic.InvokeGet(item.applications[0].settings, "c"));

            // this property is defined in the linked document
            Assert.AreEqual("2", Dynamic.InvokeGet(item.applications[0].settings, "b"));
        }
    }

    [TestFixture]
    public class NegativeTests
    {
        [TestCase]
        public void CircularReferencesShouldCauseAnException()
        {
            var repoPath = Path.GetFullPath(@"..\..\_testdata\set2");

            var jsonFiles = Directory.GetFiles(repoPath, "*.json", SearchOption.AllDirectories);

            var repo = new DynamicRepository(repoPath);
            var jsonDocuments = Helpers.ReadAllJsonFiles(jsonFiles, repoPath, "");

            Assert.Throws<CircularReferenceException>(() => repo.ProcessAllLinks(jsonDocuments));
        }
    }
}
