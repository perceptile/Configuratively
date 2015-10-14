using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Configuratively.Domain;
using Configuratively.Hosting;
using Configuratively.Infrastructure;
using Configuratively.Repositories;

namespace Configuratively.Workers
{
    internal class DirectorySync : IDisposable
    {
        private readonly ConfigSettings _settings;
        private IDisposable _disposable;
        private const int IntervalInSeconds = 30;

        public DirectorySync(ConfigSettings settings)
        {
            _settings = settings;
        }

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
                // Process the source config repo files
                var crInfo = Directory.GetFiles(_settings.RepositoryPath, "*.json", SearchOption.AllDirectories);

                // Clear-down the cache before updating it (so any deletions are honoured)
                //InMemoryRepository.ClearStandByCache();

                // Process the json files
                var dr = new DynamicRepository(_settings.RepositoryPath);
                var jsonDocuments = Helpers.ReadAllJsonFiles(crInfo, _settings.RepositoryPath, "");
                dr.ProcessAllLinks(jsonDocuments);

                // Construct our dynamic query taxonomy
                var entities = (new MappingManager(_settings)).Entities;
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


