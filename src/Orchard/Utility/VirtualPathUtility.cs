using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Orchard.Utility
{
    /// <summary>
    /// Virtual path manipulation helpers (replaces System.Web.VirtualPathUtility).
    /// </summary>
    /// <remarks>
    /// The application virtual path is the path base of the current request without the tenant url prefix,
    /// as recorded by the Orchard host middleware (see <see cref="ApplicationPathBaseKey"/>), or "/" outside of a request.
    /// </remarks>
    public static class VirtualPathUtility
    {
        /// <summary>
        /// The key of the <see cref="HttpContext.Items"/> entry holding the application path base (without the tenant prefix).
        /// </summary>
        public const string ApplicationPathBaseKey = "Orchard.ApplicationPathBase";

        private static readonly HttpContextAccessor _httpContextAccessor = new HttpContextAccessor();

        public static string ApplicationVirtualPath
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    if (httpContext.Items.TryGetValue(ApplicationPathBaseKey, out var pathBase) && pathBase is string applicationPath && applicationPath.Length > 0)
                    {
                        return applicationPath;
                    }
                }
                return "/";
            }
        }

        public static bool IsAppRelative(string virtualPath)
        {
            if (virtualPath == null)
                throw new ArgumentNullException(nameof(virtualPath));

            return virtualPath.StartsWith("~", StringComparison.Ordinal)
                && (virtualPath.Length == 1 || virtualPath[1] == '/' || virtualPath[1] == '\\');
        }

        public static bool IsAbsolute(string virtualPath)
        {
            if (virtualPath == null)
                throw new ArgumentNullException(nameof(virtualPath));

            return virtualPath.StartsWith("/", StringComparison.Ordinal) || virtualPath.StartsWith("\\", StringComparison.Ordinal);
        }

        public static string ToAbsolute(string virtualPath)
        {
            return ToAbsolute(virtualPath, ApplicationVirtualPath);
        }

        public static string ToAbsolute(string virtualPath, string applicationPath)
        {
            if (IsAbsolute(virtualPath))
                return Normalize(virtualPath);

            if (!IsAppRelative(virtualPath))
                throw new ArgumentException(string.Format("The relative virtual path '{0}' is not allowed here.", virtualPath), nameof(virtualPath));

            var root = string.IsNullOrEmpty(applicationPath) ? "/" : applicationPath;
            return Normalize(root.TrimEnd('/') + "/" + virtualPath.Substring(1).TrimStart('/', '\\'));
        }

        public static string ToAppRelative(string virtualPath)
        {
            return ToAppRelative(virtualPath, ApplicationVirtualPath);
        }

        public static string ToAppRelative(string virtualPath, string applicationPath)
        {
            if (IsAppRelative(virtualPath))
                return "~" + Normalize(virtualPath.Substring(1).Length == 0 ? "/" : virtualPath.Substring(1));

            if (!IsAbsolute(virtualPath))
                return virtualPath;

            var path = Normalize(virtualPath);
            var root = string.IsNullOrEmpty(applicationPath) ? "/" : applicationPath.TrimEnd('/');
            if (root.Length == 0)
                return "~" + path;

            if (path.Equals(root, StringComparison.OrdinalIgnoreCase))
                return "~/";

            if (path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
                return "~" + path.Substring(root.Length);

            return path;
        }

        public static string Combine(string basePath, string relativePath)
        {
            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (IsAbsolute(relativePath) || IsAppRelative(relativePath))
                return relativePath;

            var appRelative = IsAppRelative(basePath);
            var basePart = appRelative ? basePath.Substring(1) : basePath;
            var directory = basePart.Substring(0, basePart.LastIndexOf('/') + 1);
            var combined = Normalize(directory + relativePath);
            return appRelative ? "~" + combined : combined;
        }

        public static string AppendTrailingSlash(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return virtualPath;

            return virtualPath.EndsWith("/", StringComparison.Ordinal) ? virtualPath : virtualPath + "/";
        }

        public static string RemoveTrailingSlash(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath) || virtualPath.Length == 1)
                return virtualPath;

            return virtualPath.EndsWith("/", StringComparison.Ordinal) ? virtualPath.Substring(0, virtualPath.Length - 1) : virtualPath;
        }

        /// <summary>
        /// Resolves the "." and ".." segments of a rooted path, and converts the backslashes.
        /// </summary>
        private static string Normalize(string path)
        {
            var hasTrailingSlash = path.EndsWith("/", StringComparison.Ordinal) || path.EndsWith("\\", StringComparison.Ordinal);
            var segments = new List<string>();
            foreach (var segment in path.Replace('\\', '/').Split('/'))
            {
                if (segment == "..")
                {
                    if (segments.Count == 0)
                        throw new ArgumentException(string.Format("Cannot use a leading .. to exit above the top directory: '{0}'.", path));
                    segments.RemoveAt(segments.Count - 1);
                }
                else if (segment != "." && segment.Length > 0)
                {
                    segments.Add(segment);
                }
            }

            var result = "/" + string.Join("/", segments);
            if (hasTrailingSlash && segments.Count > 0)
                result += "/";
            return result;
        }
    }
}
