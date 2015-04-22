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
            var repoPath = Path.GetFullPath(@"..\..\_testdata\scenario1");

            var jsonFiles = Directory.GetFiles(repoPath, "*.json", SearchOption.AllDirectories);

            _repo = new DynamicRepository(repoPath);
            var jsonDocuments = Helpers.ReadAllJsonFiles(jsonFiles, repoPath, "");
            _repo.ProcessAllLinks(jsonDocuments);
        }

        [TestCase]
        public void SingleDepthLinksShouldBeResolved()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "root.json");

            Assert.IsNotNull(item.settings);
            Assert.IsNotNull(item.settings.numCores);
            Assert.AreEqual(2, item.settings.numCores);
        }

        [TestCase]
        public void NestedLinksShouldBeResolve()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "root.json");

            Assert.IsNotNull(item.settings);
            Assert.IsNotNull(item.settings.usesCertificate);
            Assert.AreEqual(true, item.settings.usesCertificate);
        }

        [TestCase]
        public void RootScalarValuesShouldOverrideLinkedEquivalents()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "root.json");

            Assert.IsNotNull(item.settings);
            Assert.IsNotNull(item.settings.memoryGb);
            Assert.AreEqual(8, item.settings.memoryGb);
        }

        [TestCase]
        public void ObjectsThatExistInRootAndLinksShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "root.json");

            Assert.IsNotNull(item.settings);
            Assert.IsNotNull(item.settings.usesCertificate);
            Assert.IsNotNull(item.settings.certThumbprint);
            Assert.AreEqual(true, item.settings.usesCertificate);
            Assert.AreEqual("0a52e0c3b55f4dfef40f5737120e604dbfecf947", item.settings.certThumbprint);
        }

        [TestCase]
        public void ComplexCollectionTypesThatExistInLinksAndLocallyShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "root.json");

            // TODO: Ordering of array members is being reversed, compared to what want
            Assert.IsNotNull(item.settings.drives);
            Assert.AreEqual(3, item.settings.drives.Length);
            Assert.AreEqual("OS", item.settings.drives[2].name);
            Assert.AreEqual("SWAP", item.settings.drives[1].name);
            Assert.AreEqual(20, item.settings.drives[1].sizeGb);
            Assert.AreEqual("DATA", item.settings.drives[0].name);
        }

        //[TestCase]
        //public void OverlappingComplexCollectionTypesThatExistInLinksAndLocallyShouldBeMerged()
        //{
        //    var sut = _repo.Repo;

        //    dynamic item = sut.First(i => i._id == "environments/test01.json");

        //    Assert.IsNotNull(item.networks);
        //    Assert.AreEqual(2, item.networks.Length);
        //    Assert.AreEqual("direct", item.networks[1].type);
        //    Assert.AreEqual("true", item.networks[1].disabled);
        //}

        //[TestCase]
        //public void OverlappingComplexCollectionTypesAcrossMultipleTemplatesShouldBeMerged()
        //{
        //    var sut = _repo.Repo;

        //    dynamic item = sut.First(i => i._id == "environments/dev01.json");

        //    Assert.IsNotNull(item.networks);
        //    Assert.AreEqual(2, item.networks.Length);
        //    Assert.AreEqual("routed", item.networks[1].type);
        //}

        //[TestCase]
        //public void ComplexTypesThatExistInLinksAndLocallyShouldBeMerged()
        //{
        //    var sut = _repo.Repo;

        //    dynamic item = sut.First(i => i._id == "environments/dev01.json");

        //    Assert.AreEqual("bar", item.settings.foo);
        //    Assert.AreEqual("foo", item.settings.bar);
        //}

        [TestCase]
        public void NestedArraysWithinObjectsShouldBeMerged()
        {
            var sut = _repo.Repo;

            dynamic item = sut.First(i => i._id == "root.json");

            Assert.IsNotNull(item.applications);
            Assert.AreEqual(1, item.applications.Length);
            Assert.AreEqual("notepad++", item.applications[0].name);
            Assert.IsNotNull(item.applications[0].options);
            
            // this property is overridden in the root document
            Assert.AreEqual(@"D:\NotepadPlusPlus", Dynamic.InvokeGet(item.applications[0].options, "installPath"));

            // this property only exists in the root document
            Assert.AreEqual("dark", Dynamic.InvokeGet(item.applications[0].options, "theme"));

            // this property is defined in the linked document
            Assert.AreEqual("Full", Dynamic.InvokeGet(item.applications[0].options, "installType"));
        }
    }

    [TestFixture]
    public class NegativeTests
    {
        [TestCase]
        public void CircularReferencesShouldCauseAnException()
        {
            var repoPath = Path.GetFullPath(@"..\..\_testdata\scenario2");

            var jsonFiles = Directory.GetFiles(repoPath, "*.json", SearchOption.AllDirectories);

            var repo = new DynamicRepository(repoPath);
            var jsonDocuments = Helpers.ReadAllJsonFiles(jsonFiles, repoPath, "");

            Assert.Throws<CircularReferenceException>(() => repo.ProcessAllLinks(jsonDocuments));
        }
    }
}
