using System.IO;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.MediaLibrary.Models;

namespace Orchard.MediaLibrary.Factories
{

    public class ImageFactorySelector : IMediaFactorySelector
    {
        private readonly IContentManager _contentManager;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public ImageFactorySelector(IContentManager contentManager, IContentDefinitionManager contentDefinitionManager)
        {
            _contentManager = contentManager;
            _contentDefinitionManager = contentDefinitionManager;
        }

        // The image formats which can be decoded (formerly the GDI+ image decoders).
        private static readonly string[] SupportedImageMimeTypes = {
            "image/bmp", "image/jpeg", "image/gif", "image/x-emf", "image/x-wmf", "image/tiff", "image/png", "image/x-icon", "image/webp"
        };

        public MediaFactorySelectorResult GetMediaFactory(Stream stream, string mimeType, string contentType)
        {
            if (!mimeType.StartsWith("image/"))
            {
                return null;
            }
            if (!SupportedImageMimeTypes.Contains(mimeType))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(contentType))
            {
                var contentDefinition = _contentDefinitionManager.GetTypeDefinition(contentType);
                if (contentDefinition == null || contentDefinition.Parts.All(x => x.PartDefinition.Name != typeof(ImagePart).Name))
                {
                    return null;
                }
            }

            return new MediaFactorySelectorResult
            {
                Priority = -5,
                MediaFactory = new ImageFactory(_contentManager)
            };
        }
    }

    public class ImageFactory : IMediaFactory
    {
        private readonly IContentManager _contentManager;

        public ImageFactory(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public MediaPart CreateMedia(Stream stream, string path, string mimeType, string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "Image";
            }

            var part = _contentManager.New<MediaPart>(contentType);

            part.LogicalType = "Image";
            part.MimeType = mimeType;
            part.Title = Path.GetFileNameWithoutExtension(path);

            var imagePart = part.As<ImagePart>();
            if (imagePart == null)
            {
                return null;
            }

            return part;
        }
    }
}