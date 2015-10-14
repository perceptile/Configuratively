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
using LibGit2Sharp;

namespace Configuratively.Workers
{
    internal class GitSync : IDisposable
    {
        private readonly ConfigSettings _settings;
        private IDisposable _disposable;
        private const int IntervalInSeconds = 30;

        public GitSync(ConfigSettings settings)
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
                var repositoryLocation = new DirectoryInfo(".local");

                IRepository repo;
                if (repositoryLocation.Exists)
                {
                    repo = new Repository(repositoryLocation.FullName);
                    repo.Fetch("origin", new FetchOptions());
                }
                else
                {
                    Repository.Clone(@"https://github.com/perceptile/Configuratively.git", ".local");
                    repo = new Repository(repositoryLocation.FullName);
                }
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


