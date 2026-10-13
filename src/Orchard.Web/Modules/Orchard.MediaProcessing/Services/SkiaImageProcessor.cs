using System;
using System.IO;
using SkiaSharp;

namespace Orchard.MediaProcessing.Services
{
    public enum ImageFitMode { Max, Pad, Crop, Stretch }

    public enum ImageScaleMode { DownscaleOnly, UpscaleOnly, Both, UpscaleCanvas }

    public enum ImageAnchor { TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight }

    /// <summary>
    /// Image resizing and encoding with SkiaSharp (formerly ImageResizer, which relied on System.Drawing).
    /// The resize modes follow the ImageResizer semantics (max, pad, crop, stretch and the scale modes).
    /// </summary>
    public static class SkiaImageProcessor
    {
        public static Stream Resize(Stream input, int width, int height, ImageFitMode mode, ImageScaleMode scale, ImageAnchor anchor, SKColor? background, int quality = 90)
        {
            using (var data = SKData.Create(input))
            using (var codec = SKCodec.Create(data))
            using (var source = SKBitmap.Decode(data))
            {
                if (source == null)
                {
                    throw new ArgumentException("The image format is not supported.");
                }

                var format = codec != null ? codec.EncodedFormat : SKEncodedImageFormat.Png;
                var sourceWidth = source.Width;
                var sourceHeight = source.Height;

                // unconstrained dimensions keep the image ratio
                var targetWidth = width > 0 ? width : (height > 0 ? (int)Math.Round(sourceWidth * (double)height / sourceHeight) : sourceWidth);
                var targetHeight = height > 0 ? height : (width > 0 ? (int)Math.Round(sourceHeight * (double)width / sourceWidth) : sourceHeight);

                double scaleX = (double)targetWidth / sourceWidth;
                double scaleY = (double)targetHeight / sourceHeight;

                int imageWidth, imageHeight, canvasWidth, canvasHeight;
                switch (mode)
                {
                    case ImageFitMode.Stretch:
                        imageWidth = targetWidth;
                        imageHeight = targetHeight;
                        canvasWidth = targetWidth;
                        canvasHeight = targetHeight;
                        break;
                    case ImageFitMode.Crop:
                        {
                            var ratio = Math.Max(scaleX, scaleY);
                            imageWidth = (int)Math.Round(sourceWidth * ratio);
                            imageHeight = (int)Math.Round(sourceHeight * ratio);
                            canvasWidth = targetWidth;
                            canvasHeight = targetHeight;
                            break;
                        }
                    case ImageFitMode.Pad:
                        {
                            var ratio = Math.Min(scaleX, scaleY);
                            imageWidth = (int)Math.Round(sourceWidth * ratio);
                            imageHeight = (int)Math.Round(sourceHeight * ratio);
                            canvasWidth = targetWidth;
                            canvasHeight = targetHeight;
                            break;
                        }
                    default:
                        {
                            var ratio = Math.Min(scaleX, scaleY);
                            imageWidth = (int)Math.Round(sourceWidth * ratio);
                            imageHeight = (int)Math.Round(sourceHeight * ratio);
                            canvasWidth = imageWidth;
                            canvasHeight = imageHeight;
                            break;
                        }
                }

                var upscaling = imageWidth > sourceWidth || imageHeight > sourceHeight;
                if ((upscaling && (scale == ImageScaleMode.DownscaleOnly || scale == ImageScaleMode.UpscaleCanvas))
                    || (!upscaling && scale == ImageScaleMode.UpscaleOnly))
                {
                    // the image is kept at its original size
                    imageWidth = sourceWidth;
                    imageHeight = sourceHeight;
                    if (scale != ImageScaleMode.UpscaleCanvas || mode == ImageFitMode.Max)
                    {
                        canvasWidth = mode == ImageFitMode.Crop ? Math.Min(canvasWidth, sourceWidth) : sourceWidth;
                        canvasHeight = mode == ImageFitMode.Crop ? Math.Min(canvasHeight, sourceHeight) : sourceHeight;
                    }
                }

                var info = new SKImageInfo(Math.Max(1, canvasWidth), Math.Max(1, canvasHeight), SKColorType.Rgba8888, SKAlphaType.Premul);
                using (var surface = SKSurface.Create(info))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(background ?? SKColors.Transparent);

                    var x = Align(canvasWidth - imageWidth, anchor, horizontal: true);
                    var y = Align(canvasHeight - imageHeight, anchor, horizontal: false);

                    using (var image = SKImage.FromBitmap(source))
                    using (var paint = new SKPaint { IsAntialias = true })
                    {
                        canvas.DrawImage(image, new SKRect(x, y, x + imageWidth, y + imageHeight), new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
                    }

                    using (var snapshot = surface.Snapshot())
                    {
                        return Encode(snapshot, format, quality);
                    }
                }
            }
        }

        public static Stream ChangeFormat(Stream input, string format, int quality)
        {
            using (var source = SKBitmap.Decode(input))
            {
                if (source == null)
                {
                    throw new ArgumentException("The image format is not supported.");
                }

                using (var image = SKImage.FromBitmap(source))
                {
                    return Encode(image, ParseFormat(format), quality);
                }
            }
        }

        public static SKEncodedImageFormat ParseFormat(string format)
        {
            switch ((format ?? string.Empty).Trim('.').ToLowerInvariant())
            {
                case "jpg":
                case "jpeg":
                    return SKEncodedImageFormat.Jpeg;
                case "gif":
                    return SKEncodedImageFormat.Gif;
                case "bmp":
                    return SKEncodedImageFormat.Bmp;
                case "webp":
                    return SKEncodedImageFormat.Webp;
                default:
                    return SKEncodedImageFormat.Png;
            }
        }

        private static Stream Encode(SKImage image, SKEncodedImageFormat format, int quality)
        {
            // SkiaSharp can only encode JPEG, PNG and WEBP images, the other formats are encoded as PNG.
            if (format != SKEncodedImageFormat.Jpeg && format != SKEncodedImageFormat.Webp)
            {
                format = SKEncodedImageFormat.Png;
            }

            var data = image.Encode(format, Math.Max(0, Math.Min(100, quality)));
            var result = new MemoryStream();
            data.SaveTo(result);
            result.Seek(0, SeekOrigin.Begin);
            return result;
        }

        private static float Align(int space, ImageAnchor anchor, bool horizontal)
        {
            int position;
            if (horizontal)
            {
                position = anchor == ImageAnchor.TopLeft || anchor == ImageAnchor.MiddleLeft || anchor == ImageAnchor.BottomLeft ? 0
                    : anchor == ImageAnchor.TopRight || anchor == ImageAnchor.MiddleRight || anchor == ImageAnchor.BottomRight ? 2 : 1;
            }
            else
            {
                position = anchor == ImageAnchor.TopLeft || anchor == ImageAnchor.TopCenter || anchor == ImageAnchor.TopRight ? 0
                    : anchor == ImageAnchor.BottomLeft || anchor == ImageAnchor.BottomCenter || anchor == ImageAnchor.BottomRight ? 2 : 1;
            }

            return position == 0 ? 0 : position == 2 ? space : space / 2f;
        }
    }
}
