using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Orchard.Environment;
using Orchard.Caching;
using Orchard.Logging;
using Orchard.Services;

namespace Orchard.FileSystems.VirtualPath
{

    public class DefaultVirtualPathMonitor : IVirtualPathMonitor
    {
        private readonly Thunk _thunk;
        private readonly string _prefix = Guid.NewGuid().ToString("n");
        private readonly IDictionary<string, Weak<Token>> _tokens = new Dictionary<string, Weak<Token>>();
        private readonly IClock _clock;
        private readonly HashSet<string> _watched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PhysicalFileProvider> _fileProviders = new Dictionary<string, PhysicalFileProvider>(StringComparer.OrdinalIgnoreCase);

        public DefaultVirtualPathMonitor(IClock clock)
        {
            _clock = clock;
            _thunk = new Thunk(this);
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public IVolatileToken WhenPathChanges(string virtualPath)
        {
            var token = BindToken(virtualPath);
            try
            {
                BindSignal(virtualPath);
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                // This exception happens if trying to monitor a directory or file
                // inside a directory which doesn't exist
                Logger.Information(e, "Error monitoring file changes on virtual path '{0}'", virtualPath);

                //TODO: Return a token monitoring first existing parent directory.
            }
            return token;
        }

        private Token BindToken(string virtualPath)
        {
            lock (_tokens)
            {
                Weak<Token> weak;
                if (!_tokens.TryGetValue(virtualPath, out weak))
                {
                    weak = new Weak<Token>(new Token(virtualPath));
                    _tokens[virtualPath] = weak;
                }

                var token = weak.Target;
                if (token == null)
                {
                    token = new Token(virtualPath);
                    weak.Target = token;
                }

                return token;
            }
        }

        private Token DetachToken(string virtualPath)
        {
            lock (_tokens)
            {
                Weak<Token> weak;
                if (!_tokens.TryGetValue(virtualPath, out weak))
                {
                    return null;
                }
                var token = weak.Target;
                weak.Target = null;
                return token;
            }
        }

        private void BindSignal(string virtualPath)
        {
            BindSignal(virtualPath, _thunk.Signal);

        }

        private void BindSignal(string virtualPath, Action<string> callback)
        {
            //PERF: Don't watch a path twice, watchers are expensive.
            lock (_watched)
            {
                if (!_watched.Add(virtualPath))
                    return;
            }

            var physicalPath = HostingEnvironment.MapPath(virtualPath);
            var directory = Directory.Exists(physicalPath) ? physicalPath : Path.GetDirectoryName(physicalPath);
            if (directory == null || !Directory.Exists(directory))
            {
                lock (_watched)
                {
                    _watched.Remove(virtualPath);
                }
                throw new DirectoryNotFoundException(string.Format("The directory of the virtual path '{0}' doesn't exist.", virtualPath));
            }

            var fileProvider = GetFileProvider(directory);
            var filter = Directory.Exists(physicalPath) ? "**/*" : Path.GetFileName(physicalPath);
            var changeToken = fileProvider.Watch(filter);

            Logger.Debug("Monitoring virtual path \"{0}\"", virtualPath);

            changeToken.RegisterChangeCallback(state =>
            {
                lock (_watched)
                {
                    _watched.Remove(virtualPath);
                }
                callback(virtualPath);
            }, null);
        }

        private PhysicalFileProvider GetFileProvider(string directory)
        {
            lock (_fileProviders)
            {
                if (!_fileProviders.TryGetValue(directory, out var provider))
                {
                    provider = new PhysicalFileProvider(directory);
                    _fileProviders[directory] = provider;
                }
                return provider;
            }
        }

        public void Signal(string virtualPath)
        {
            Logger.Debug("Virtual path changed '{0}'", virtualPath);

            var token = DetachToken(virtualPath);
            if (token != null)
                token.IsCurrent = false;
        }

        public class Token : IVolatileToken
        {
            public Token(string virtualPath)
            {
                IsCurrent = true;
                VirtualPath = virtualPath;
            }
            public bool IsCurrent { get; set; }
            public string VirtualPath { get; private set; }

            public override string ToString()
            {
                return string.Format("IsCurrent: {0}, VirtualPath: \"{1}\"", IsCurrent, VirtualPath);
            }
        }

        class Thunk
        {
            private readonly Weak<DefaultVirtualPathMonitor> _weak;

            public Thunk(DefaultVirtualPathMonitor provider)
            {
                _weak = new Weak<DefaultVirtualPathMonitor>(provider);
            }

            public void Signal(string virtualPath)
            {
                var provider = _weak.Target;
                if (provider != null)
                    provider.Signal(virtualPath);
            }
        }
    }
}