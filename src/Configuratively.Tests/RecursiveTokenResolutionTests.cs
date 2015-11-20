using System;
using System.IO;
using NUnit.Framework;

using Configuratively.Domain;

namespace Configuratively.Tests
{
    [TestFixture]
    public class RecursiveTokenResolutionTests
    {
        [TestCase]
        public void EntriesContainingTokenisedValuesShouldBeResolvedToActualValues()
        {
            string repoPath = Path.GetFullPath(@"..\..\_testdata\recursive-tokens");
            var files = Directory.GetFiles(repoPath, "*.json", SearchOption.AllDirectories);

            var repo = new DynamicRepository(repoPath);
            var jsonDocuments = Helpers.ReadAllJsonFiles(files, repoPath, "");
            repo.ProcessAllLinks(jsonDocuments);


            var tokenResolver = new TokenResolver();
            repo.Repo = tokenResolver.ResolveTokens(repo.Repo);

            // These tests are only for the core token replacement functionality
            // within a single hierarchy (i.e. JSON document tree), hence the indexing on repo.Repo

            // static value
            Assert.AreEqual("static-value", repo.Repo[1].simple1.ToString());

            // tokenised value referencing a string item (e.g. ##simple1##)
            Assert.AreEqual("static-value", repo.Repo[1].simple2.ToString());

            // tokenised value referencing a child item of an object the repo (e.g. ##complex.child2##)
            Assert.AreEqual("bar", repo.Repo[1].simple3.ToString());

            // tokenised value referencing an item that itself is tokenised also (i.e. a nested tokenisation)
            Assert.AreEqual("bar", repo.Repo[1].simple4.ToString());

        }
    }
}
