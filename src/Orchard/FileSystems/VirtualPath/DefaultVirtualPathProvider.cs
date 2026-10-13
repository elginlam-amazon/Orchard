using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orchard.Environment;
using Orchard.Exceptions;
using Orchard.Logging;
using Orchard.Utility;

namespace Orchard.FileSystems.VirtualPath
{
    public class DefaultVirtualPathProvider : IVirtualPathProvider
    {
        public DefaultVirtualPathProvider()
        {
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public virtual string GetDirectoryName(string virtualPath)
        {
            return Path.GetDirectoryName(virtualPath).Replace(Path.DirectorySeparatorChar, '/');
        }

        public virtual IEnumerable<string> ListFiles(string path)
        {
            var directory = ToAppRelativeDirectory(path);
            var physicalPath = MapPath(directory);
            if (!Directory.Exists(physicalPath))
                return Enumerable.Empty<string>();

            return Directory
                .GetFiles(physicalPath)
                .Select(f => directory + Path.GetFileName(f));
        }

        public virtual IEnumerable<string> ListDirectories(string path)
        {
            var directory = ToAppRelativeDirectory(path);
            var physicalPath = MapPath(directory);
            if (!Directory.Exists(physicalPath))
                return Enumerable.Empty<string>();

            return Directory
                .GetDirectories(physicalPath)
                .Select(d => directory + Path.GetFileName(d) + "/");
        }

        public virtual string Combine(params string[] paths)
        {
            return Path.Combine(paths).Replace(Path.DirectorySeparatorChar, '/');
        }

        public virtual string ToAppRelative(string virtualPath)
        {
            if (IsMalformedVirtualPath(virtualPath))
                return null;

            try
            {
                string result = NormalizeAppRelative(virtualPath);

                // In some cases, ToAppRelative doesn't normalize the path. In those cases,
                // the path is invalid.
                // Example:
                //   ApplicationPath: /Foo
                //   VirtualPath    : ~/Bar/../Blah/Blah2
                //   Result         : /Blah/Blah2  <= that is not an app relative path!
                if (!result.StartsWith("~/"))
                {
                    Logger.Information("Path '{0}' cannot be made app relative: Path returned ('{1}') is not app relative.", virtualPath, result);
                    return null;
                }
                return result;
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                // The initial path might have been invalid (e.g. path indicates a path outside the application root)
                Logger.Information(ex, "Path '{0}' cannot be made app relative", virtualPath);
                return null;
            }
        }

        /// <summary>
        /// We want to reject path that contains ".." going outside of the application root.
        /// ToAppRelative does that already, but we want to do the same while avoiding exceptions.
        /// 
        /// Note: This method doesn't detect all cases of malformed paths, it merely checks
        ///       for *some* cases of malformed paths, so this is not a replacement for full virtual path
        ///       verification through VirtualPathUtilty methods.
        ///       In other words, !IsMalformed does *not* imply "IsWellformed".
        /// </summary>
        public bool IsMalformedVirtualPath(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return true;

            if (virtualPath.IndexOf("..") >= 0)
            {
                virtualPath = virtualPath.Replace(Path.DirectorySeparatorChar, '/');
                string rootPrefix = virtualPath.StartsWith("~/") ? "~/" : virtualPath.StartsWith("/") ? "/" : "";
                if (!string.IsNullOrEmpty(rootPrefix))
                {
                    string[] terms = virtualPath.Substring(rootPrefix.Length).Split('/');
                    int depth = 0;
                    foreach (var term in terms)
                    {
                        if (term == "..")
                        {
                            if (depth == 0)
                            {
                                Logger.Information("Path '{0}' cannot be made app relative: Too many '..'", virtualPath);
                                return true;
                            }
                            depth--;
                        }
                        else
                        {
                            depth++;
                        }
                    }
                }
            }

            return false;
        }

        public virtual Stream OpenFile(string virtualPath)
        {
            return File.OpenRead(MapPath(virtualPath));
        }

        public virtual StreamWriter CreateText(string virtualPath)
        {
            return File.CreateText(MapPath(virtualPath));
        }

        public virtual Stream CreateFile(string virtualPath)
        {
            return File.Create(MapPath(virtualPath));
        }

        public virtual DateTime GetFileLastWriteTimeUtc(string virtualPath)
        {
#if true
            // This code is less "pure" than the code below, but performs fewer file I/O, and it 
            // has been measured to make a significant difference (4x) on slow file systems.
            return File.GetLastWriteTimeUtc(MapPath(virtualPath));
#else
            return File.GetLastWriteTimeUtc(MapPath(virtualPath));
#endif
        }

        public string GetFileHash(string virtualPath)
        {
            return GetFileHash(virtualPath, new[] { virtualPath });
        }

        public string GetFileHash(string virtualPath, IEnumerable<string> dependencies)
        {
            // Hash of the last write times and sizes of the files (the former ASP.NET virtual path provider file hash).
            var hash = new HashCode();
            foreach (var dependency in dependencies.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var physicalPath = MapPath(dependency);
                hash.Add(dependency.ToLowerInvariant());
                if (File.Exists(physicalPath))
                {
                    var fileInfo = new FileInfo(physicalPath);
                    hash.Add(fileInfo.LastWriteTimeUtc.Ticks);
                    hash.Add(fileInfo.Length);
                }
                else if (Directory.Exists(physicalPath))
                {
                    hash.Add(Directory.GetLastWriteTimeUtc(physicalPath).Ticks);
                }
            }
            return hash.ToHashCode().ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
        }

        public virtual void DeleteFile(string virtualPath)
        {
            File.Delete(MapPath(virtualPath));
        }

        public virtual string MapPath(string virtualPath)
        {
            return HostingEnvironment.MapPath(virtualPath);
        }

        public virtual bool FileExists(string virtualPath)
        {
            return File.Exists(MapPath(virtualPath));
        }

        public virtual bool TryFileExists(string virtualPath)
        {
            if (IsMalformedVirtualPath(virtualPath))
                return false;

            try
            {
                return FileExists(virtualPath);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                Logger.Information(ex, "File '{0}' can not be checked for existence. Assuming doesn't exist.", virtualPath);
                return false;
            }
        }

        public virtual bool DirectoryExists(string virtualPath)
        {
            return Directory.Exists(MapPath(virtualPath));
        }

        public virtual void CreateDirectory(string virtualPath)
        {
            Directory.CreateDirectory(MapPath(virtualPath));
        }

        private string ToAppRelativeDirectory(string virtualPath)
        {
            var directory = NormalizeAppRelative(virtualPath);
            return directory.EndsWith("/") ? directory : directory + "/";
        }

        /// <summary>
        /// Converts a rooted (/...) or app relative (~/...) virtual path to its normalized app relative form,
        /// resolving the "." and ".." segments (the former VirtualPathUtility.ToAppRelative).
        /// </summary>
        private static string NormalizeAppRelative(string virtualPath)
        {
            var path = virtualPath.Replace('\\', '/');
            if (path.StartsWith("~/"))
            {
                path = path.Substring(1);
            }
            else if (path == "~")
            {
                path = "/";
            }
            else if (!path.StartsWith("/"))
            {
                throw new ArgumentException(string.Format("The virtual path '{0}' is not rooted.", virtualPath));
            }

            var segments = new List<string>();
            foreach (var segment in path.Split('/'))
            {
                if (segment == "..")
                {
                    if (segments.Count == 0)
                        throw new ArgumentException(string.Format("The virtual path '{0}' is outside of the application.", virtualPath));
                    segments.RemoveAt(segments.Count - 1);
                }
                else if (segment != "." && segment.Length > 0)
                {
                    segments.Add(segment);
                }
            }

            var result = "~/" + string.Join("/", segments);
            if (path.EndsWith("/") && segments.Count > 0)
            {
                result += "/";
            }
            return result;
        }

        public virtual void DeleteDirectory(string virtualPath)
        {
            Directory.Delete(MapPath(virtualPath));
        }
    }
}