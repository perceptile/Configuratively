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
                .Subscribe(l => Synchronise(ConfigurationManager.AppSettings["repoPath"]), e => LogManager.Logger.Error(e));
        }

        public void Synchronise(string repositoryPath)
        {
            try
            {
                var repositoryFullPath = Path.GetFullPath(repositoryPath);

                // Process the source config repo files
                var crInfo = Directory.GetFiles(repositoryFullPath, "*.json", SearchOption.AllDirectories);

                // Clear-down the cache before updating it (so any deletions are honoured)
                //InMemoryRepository.ClearStandByCache();

                // Process the json files
                var dr = new DynamicRepository(repositoryFullPath);
                var jsonDocuments = Helpers.ReadAllJsonFiles(crInfo, repositoryFullPath, "");
                dr.ProcessAllLinks(jsonDocuments);

                // Construct our dynamic query taxonomy
                var entities = (new MappingManager()).Entities;
                foreach (var e in entities)
                {
                    // Simple map
                    if (!string.IsNullOrEmpty(e.Regex))
                    {
                        IEnumerable<dynamic> items = dr.Repo.Where(i => Regex.IsMatch(i._id.ToString(), e.Regex)).ToList();
                        InMemoryRepository.Persist(e.Name, items);
                    }
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


