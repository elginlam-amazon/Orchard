using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Orchard.Mvc.ViewEngines.Razor
{
    /// <summary>
    /// An in-memory file provider for the Razor views which are not files (e.g. templates stored in the database).
    /// It is registered in the Razor runtime compilation file providers by the host.
    /// </summary>
    public class DynamicViewFileProvider : IFileProvider
    {
        public static DynamicViewFileProvider Instance { get; } = new DynamicViewFileProvider();

        private readonly ConcurrentDictionary<string, DynamicFile> _files = new ConcurrentDictionary<string, DynamicFile>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Adds or updates a view and returns its path (rooted, e.g. /_Dynamic/Template.cshtml).
        /// </summary>
        public string AddOrUpdate(string path, string content)
        {
            path = Normalize(path);
            var existing = _files.TryGetValue(path, out var file) ? file : null;
            if (existing != null && existing.Content == content)
            {
                return path;
            }

            _files[path] = new DynamicFile(Path.GetFileName(path), content);

            // notify the Razor compiler that the view has changed
            if (_tokens.TryRemove(path, out var tokenSource))
            {
                tokenSource.Cancel();
            }

            return path;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return _files.TryGetValue(Normalize(subpath), out var file) ? (IFileInfo)file : new NotFoundFileInfo(subpath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        public IChangeToken Watch(string filter)
        {
            var path = Normalize(filter);
            if (!_files.ContainsKey(path))
            {
                return NullChangeToken.Singleton;
            }

            var tokenSource = _tokens.GetOrAdd(path, key => new CancellationTokenSource());
            return new CancellationChangeToken(tokenSource.Token);
        }

        private static string Normalize(string path)
        {
            path = (path ?? string.Empty).Replace('\\', '/');
            if (path.StartsWith("~"))
            {
                path = path.Substring(1);
            }
            return path.StartsWith("/") ? path : "/" + path;
        }

        private class DynamicFile : IFileInfo
        {
            private readonly byte[] _bytes;

            public DynamicFile(string name, string content)
            {
                Name = name;
                Content = content;
                _bytes = Encoding.UTF8.GetBytes(content);
                LastModified = DateTimeOffset.UtcNow;
            }

            public string Content { get; }
            public bool Exists => true;
            public long Length => _bytes.Length;
            public string PhysicalPath => null;
            public string Name { get; }
            public DateTimeOffset LastModified { get; }
            public bool IsDirectory => false;
            public Stream CreateReadStream() => new MemoryStream(_bytes, false);
        }
    }
}
