using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;

namespace Orchard.Packaging.GalleryServer
{
    /// <summary>
    /// Reads the packages of an Orchard gallery OData (Atom) feed.
    /// </summary>
    /// <remarks>
    /// Replaces the WCF Data Services client proxy (System.Data.Services.Client is not available on .NET):
    /// the feed entries are downloaded and the LINQ queries are evaluated in memory.
    /// </remarks>
    public class GalleryFeedContext
    {
        private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace DataServices = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace Metadata = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        private readonly Uri _serviceRoot;
        private readonly Dictionary<PublishedPackage, Uri> _readStreamUris = new Dictionary<PublishedPackage, Uri>();

        public GalleryFeedContext(Uri serviceRoot)
        {
            _serviceRoot = serviceRoot;
        }

        /// <summary>
        /// Kept for compatibility with the former data service context, missing properties are always ignored.
        /// </summary>
        public bool IgnoreMissingProperties { get; set; }

        public PackagesQuery Packages => new PackagesQuery(this, false);

        public Uri GetReadStreamUri(PublishedPackage package)
        {
            return _readStreamUris.TryGetValue(package, out var uri) ? uri : null;
        }

        private IList<PublishedPackage> LoadPackages(bool expandScreenshots)
        {
            var packages = new List<PublishedPackage>();
            var url = new Uri(_serviceRoot.ToString().TrimEnd('/') + "/Packages" + (expandScreenshots ? "?$expand=Screenshots" : string.Empty));

            while (url != null)
            {
                var document = XDocument.Parse(_httpClient.GetStringAsync(url).GetAwaiter().GetResult());
                var feed = document.Root;

                foreach (var entry in feed.Elements(Atom + "entry"))
                {
                    var package = ParsePackage(entry);
                    var content = entry.Element(Atom + "content");
                    var source = (string)content?.Attribute("src");
                    if (!string.IsNullOrEmpty(source))
                    {
                        _readStreamUris[package] = new Uri(url, source);
                    }
                    packages.Add(package);
                }

                var next = feed.Elements(Atom + "link").FirstOrDefault(link => (string)link.Attribute("rel") == "next");
                url = next != null ? new Uri(url, (string)next.Attribute("href")) : null;
            }

            return packages;
        }

        private static PublishedPackage ParsePackage(XElement entry)
        {
            var properties = entry.Element(Metadata + "properties") ?? entry.Element(Atom + "content")?.Element(Metadata + "properties");
            string Get(string name) => (string)properties?.Element(DataServices + name);

            var package = new PublishedPackage
            {
                Id = Get("Id"),
                Version = Get("Version"),
                Title = Get("Title"),
                Authors = Get("Authors"),
                PackageType = Get("PackageType"),
                Summary = Get("Summary"),
                Description = Get("Description"),
                Copyright = Get("Copyright"),
                PackageHashAlgorithm = Get("PackageHashAlgorithm"),
                PackageHash = Get("PackageHash"),
                PackageSize = ParseLong(Get("PackageSize")),
                Price = ParseDecimal(Get("Price")),
                RequireLicenseAcceptance = ParseBool(Get("RequireLicenseAcceptance")),
                IsLatestVersion = ParseBool(Get("IsLatestVersion")),
                VersionRating = ParseDouble(Get("VersionRating")),
                VersionRatingsCount = (int)ParseLong(Get("VersionRatingsCount")),
                VersionDownloadCount = (int)ParseLong(Get("VersionDownloadCount")),
                Created = ParseDate(Get("Created")),
                LastUpdated = ParseDate(Get("LastUpdated")),
                Published = ParseDate(Get("Published")),
                ExternalPackageUrl = Get("ExternalPackageUrl"),
                ProjectUrl = Get("ProjectUrl"),
                LicenseUrl = Get("LicenseUrl"),
                IconUrl = Get("IconUrl"),
                Rating = ParseDouble(Get("Rating")),
                RatingsCount = (int)ParseLong(Get("RatingsCount")),
                DownloadCount = (int)ParseLong(Get("DownloadCount")),
                Categories = Get("Categories"),
                Tags = Get("Tags"),
                Dependencies = Get("Dependencies"),
                ReportAbuseUrl = Get("ReportAbuseUrl"),
                GalleryDetailsUrl = Get("GalleryDetailsUrl"),
            };

            // expanded screenshots are inlined in the "Screenshots" navigation link
            var screenshotsLink = entry.Elements(Atom + "link").FirstOrDefault(link => ((string)link.Attribute("title")) == "Screenshots");
            var inlineFeed = screenshotsLink?.Element(Metadata + "inline")?.Element(Atom + "feed");
            if (inlineFeed != null)
            {
                foreach (var screenshotEntry in inlineFeed.Elements(Atom + "entry"))
                {
                    var screenshotProperties = screenshotEntry.Element(Atom + "content")?.Element(Metadata + "properties") ?? screenshotEntry.Element(Metadata + "properties");
                    string GetScreenshot(string name) => (string)screenshotProperties?.Element(DataServices + name);
                    package.Screenshots.Add(new PublishedScreenshot
                    {
                        Id = (int)ParseLong(GetScreenshot("Id")),
                        PublishedPackageId = GetScreenshot("PublishedPackageId"),
                        PublishedPackageVersion = GetScreenshot("PublishedPackageVersion"),
                        ScreenshotUri = GetScreenshot("ScreenshotUri"),
                        Caption = GetScreenshot("Caption")
                    });
                }
            }

            return package;
        }

        private static long ParseLong(string value) => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
        private static double ParseDouble(string value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0;
        private static decimal ParseDecimal(string value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0;
        private static bool ParseBool(string value) => bool.TryParse(value, out var result) && result;
        private static DateTime ParseDate(string value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : DateTime.MinValue;

        /// <summary>
        /// The packages of the feed, loaded when the query is enumerated.
        /// </summary>
        public class PackagesQuery : IQueryable<PublishedPackage>
        {
            private readonly GalleryFeedContext _context;
            private readonly bool _expandScreenshots;
            private readonly Lazy<IQueryable<PublishedPackage>> _packages;

            public PackagesQuery(GalleryFeedContext context, bool expandScreenshots)
            {
                _context = context;
                _expandScreenshots = expandScreenshots;
                _packages = new Lazy<IQueryable<PublishedPackage>>(() => _context.LoadPackages(_expandScreenshots).AsQueryable());
            }

            /// <summary>
            /// Includes a navigation property in the results (only "Screenshots" is supported).
            /// </summary>
            public PackagesQuery Expand(string path)
            {
                return new PackagesQuery(_context, _expandScreenshots || string.Equals(path, "Screenshots", StringComparison.OrdinalIgnoreCase));
            }

            public Type ElementType => typeof(PublishedPackage);
            public System.Linq.Expressions.Expression Expression => _packages.Value.Expression;
            public IQueryProvider Provider => _packages.Value.Provider;
            public IEnumerator<PublishedPackage> GetEnumerator() => _packages.Value.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    public class PublishedPackage
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string PackageType { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Copyright { get; set; }
        public string PackageHashAlgorithm { get; set; }
        public string PackageHash { get; set; }
        public long PackageSize { get; set; }
        public decimal Price { get; set; }
        public bool RequireLicenseAcceptance { get; set; }
        public bool IsLatestVersion { get; set; }
        public double VersionRating { get; set; }
        public int VersionRatingsCount { get; set; }
        public int VersionDownloadCount { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Published { get; set; }
        public string ExternalPackageUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string IconUrl { get; set; }
        public double Rating { get; set; }
        public int RatingsCount { get; set; }
        public int DownloadCount { get; set; }
        public string Categories { get; set; }
        public string Tags { get; set; }
        public string Dependencies { get; set; }
        public string ReportAbuseUrl { get; set; }
        public string GalleryDetailsUrl { get; set; }
        public Collection<PublishedScreenshot> Screenshots { get; set; } = new Collection<PublishedScreenshot>();
    }

    public class PublishedScreenshot
    {
        public int Id { get; set; }
        public string PublishedPackageId { get; set; }
        public string PublishedPackageVersion { get; set; }
        public string ScreenshotUri { get; set; }
        public string Caption { get; set; }
    }
}
