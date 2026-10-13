using Microsoft.AspNetCore.StaticFiles;

namespace Orchard.FileSystems.Media
{
    /// <summary>
    /// Returns the mime-type of the specified file path.
    /// </summary>
    public class ConfigurationMimeTypeProvider : IMimeTypeProvider
    {
        private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

        /// <summary>
        /// Returns the mime-type of the specified file path.
        /// </summary>
        public string GetMimeType(string path)
        {
            // Returns "application/octet-stream" for unmapped / unkown file types.
            return _contentTypeProvider.TryGetContentType(path, out var contentType) ? contentType : "application/octet-stream";
        }
    }
}