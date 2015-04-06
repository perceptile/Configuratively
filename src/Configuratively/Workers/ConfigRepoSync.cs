using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Configuratively.Domain;
using Configuratively.Infrastructure;
using Configuratively.Repositories;

namespace Configuratively.Workers
{
    internal class ConfigRepoSync : IDisposable
    {
        private IDisposable _disposable;
        private const int IntervalInSeconds = 30;

        public void Start()
        {
            _disposable = Observable
                .Interval(TimeSpan.FromSeconds(IntervalInSeconds))
                .StartWith(0)
                .Subscribe(l => Synchronise(), e => LogManager.Logger.Error(e));
        }

        public void Synchronise()
        {
            try
            {
                var repoPath = Path.GetFullPath(ConfigurationManager.AppSettings["repoPath"]);

                // Process the source config repo files
                var crInfo = Directory.GetFiles(repoPath, "*.json", SearchOption.AllDirectories);

                // Clear-down the cache before updating it (so any deletions are honoured)
                //InMemoryRepository.ClearStandByCache();

                // Process the json files
                var dr = new DynamicRepository(repoPath);
                var jsonDocuments = Helpers.ReadAllJsonFiles(crInfo, repoPath, "");
                dr.ProcessAllLinks(jsonDocuments);

                // Construct our dynamic query taxonomy
                var entities = (new MappingManager()).Entities;
                foreach (var e in entities.Keys)
                {
                    IEnumerable<dynamic> envs = dr.Repo.Where(i => Regex.Match(i._id.ToString(), entities[e]).Success == true).ToList();
                    InMemoryRepository.Persist(e, envs);
                }

                // Now make the changes we've made above live
                //InMemoryRepository.SwapActiveCache();
            }
            catch (Exception e)
            {
                LogManager.Logger.Error(e);
            }
        }

        public void Dispose()
        {
            if(_disposable != null)
                _disposable.Dispose();
        }
    }
}