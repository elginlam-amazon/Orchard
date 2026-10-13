using System;
using System.IO;
using SkiaSharp;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.MediaProcessing.Descriptors.Filter;
using Orchard.MediaProcessing.Services;

namespace Orchard.MediaProcessing.Providers.Filters
{
    public class ResizeFilter : IImageFilterProvider
    {
        public ResizeFilter()
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Describe(DescribeFilterContext describe)
        {
            describe.For("Transform", T("Transform"), T("Transform"))
                .Element("Resize", T("Resize"), T("Resizes using predefined height or width."),
                         ApplyFilter,
                         DisplayFilter,
                         "ResizeFilter"
                );
        }

        public void ApplyFilter(FilterContext context)
        {
            int witdh = context.State.Width;
            int height = context.State.Height;
            string mode = context.State.Mode;
            string alignment = context.State.Alignment;
            string padcolor = context.State.PadColor;
            string scale = context.State.Scale;

            var fitMode = ImageFitMode.Max;
            var scaleMode = ImageScaleMode.DownscaleOnly;
            var anchor = ImageAnchor.MiddleCenter;
            SKColor? backgroundColor = null;

            switch (mode)
            {
                case "max": fitMode = ImageFitMode.Max; break;
                case "pad":
                    fitMode = ImageFitMode.Pad;
                    scaleMode = ImageScaleMode.Both;
                    break;
                case "crop": fitMode = ImageFitMode.Crop; break;
                case "stretch": fitMode = ImageFitMode.Stretch; break;
            }

            switch (alignment)
            {
                case "topleft": anchor = ImageAnchor.TopLeft; break;
                case "topcenter": anchor = ImageAnchor.TopCenter; break;
                case "topright": anchor = ImageAnchor.TopRight; break;
                case "middleleft": anchor = ImageAnchor.MiddleLeft; break;
                case "middlecenter": anchor = ImageAnchor.MiddleCenter; break;
                case "middleright": anchor = ImageAnchor.MiddleRight; break;
                case "bottomleft": anchor = ImageAnchor.BottomLeft; break;
                case "bottomcenter": anchor = ImageAnchor.BottomCenter; break;
                case "bottomright": anchor = ImageAnchor.BottomRight; break;
            }

            if (!string.IsNullOrWhiteSpace(padcolor))
            {
                // html (#rrggbb) or named colors
                if (SKColor.TryParse(padcolor, out var color) || SKColor.TryParse(System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromName(padcolor)), out color))
                {
                    backgroundColor = color;
                }
            }

            switch (scale)
            {
                case "downscaleOnly": scaleMode = ImageScaleMode.DownscaleOnly; break;
                case "upscaleOnly": scaleMode = ImageScaleMode.UpscaleOnly; break;
                case "both": scaleMode = ImageScaleMode.Both; break;
                case "upscaleCanvas": scaleMode = ImageScaleMode.UpscaleCanvas; break;
            }

            if (context.Media.CanSeek)
            {
                context.Media.Seek(0, SeekOrigin.Begin);
            }
            context.Media = SkiaImageProcessor.Resize(context.Media, witdh, height, fitMode, scaleMode, anchor, backgroundColor);
        }

        public LocalizedString DisplayFilter(FilterContext context)
        {
            string mode = context.State.Mode;

            switch (mode)
            {
                case "pad": return T("Pad to {0} x {1}", context.State.Width, context.State.Height);
                case "crop": return T("Crop to {0} x {1}", context.State.Width, context.State.Height);
                case "stretch": return T("Stretch to {0} x {1}", context.State.Width, context.State.Height);
                default: return T("Resize to {0} x {1}", context.State.Width, context.State.Height);

            }
        }
    }

    public class ResizeFilterForms : IFormProvider
    {
        protected dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public ResizeFilterForms(
            IShapeFactory shapeFactory)
        {
            Shape = shapeFactory;
            T = NullLocalizer.Instance;
        }

        public void Describe(DescribeContext context)
        {
            Func<IShapeFactory, object> form =
                shape =>
                {
                    var f = Shape.Form(
                        Id: "ImageResizeFilter",
                        _Width: Shape.Textbox(
                            Id: "width", Name: "Width",
                            Title: T("Width"),
                            Value: 0,
                            Description: T("The width in pixels."),
                            Classes: new[] { "text small" }),
                        _Height: Shape.Textbox(
                            Id: "height", Name: "Height",
                            Title: T("Height"),
                            Value: 0,
                            Description: T("The height in pixels."),
                            Classes: new[] { "text small" }),
                        _Mode: Shape.SelectList(
                            Id: "mode", Name: "Mode",
                            Title: T("Mode"),
                            Description: T("How the image should be resized.<br />Max: adjusts to the max given width or height, keeping image ratio.<br />Pad: adds a padding so that the target image is exactly of width and height.<br />Crop: removes part of the image to fit with given height and width.<br />Stretch: stretches the image to fit within height and width."),
                            Size: 1,
                            Multiple: false),
                        _Alignment: Shape.SelectList(
                            Id: "alignment", Name: "Alignment",
                            Title: T("Alignment"),
                            Description: T("Select the alignment for Crop and Pad modes."),
                            Size: 1,
                            Multiple: false),
                        _PadColor: Shape.Textbox(
                            Id: "padcolor", Name: "PadColor",
                            Title: T("Pad Color"),
                            Value: "",
                            Description: T("The background color to use to pad the image e.g., #ffffff, red. Leave empty to keep transparency."),
                            Classes: new[] { "text small" }),
                        _Scale: Shape.SelectList(
                            Id: "scale", Name: "Scale",
                            Title: T("Scale"),
                            Description: T("Select the scale mode which defines whether the image is allowed to upscale, downscale, both, or if only the canvas gets to be upscaled."),
                            Size: 1,
                            Multiple: false)
                        );

                    f._Mode.Add(new SelectListItem { Value = "max", Text = T("Max").Text });
                    f._Mode.Add(new SelectListItem { Value = "pad", Text = T("Pad").Text });
                    f._Mode.Add(new SelectListItem { Value = "crop", Text = T("Crop").Text });
                    f._Mode.Add(new SelectListItem { Value = "stretch", Text = T("Stretch").Text });

                    f._Alignment.Add(new SelectListItem { Value = "topleft", Text = T("Top Left").Text });
                    f._Alignment.Add(new SelectListItem { Value = "topcenter", Text = T("Top Center").Text });
                    f._Alignment.Add(new SelectListItem { Value = "topright", Text = T("Top Right").Text });
                    f._Alignment.Add(new SelectListItem { Value = "middleleft", Text = T("Middle Left").Text });
                    f._Alignment.Add(new SelectListItem { Value = "middlecenter", Text = T("Middle Center").Text });
                    f._Alignment.Add(new SelectListItem { Value = "middleright", Text = T("Middle Right").Text });
                    f._Alignment.Add(new SelectListItem { Value = "bottomleft", Text = T("Bottom Left").Text });
                    f._Alignment.Add(new SelectListItem { Value = "bottomcenter", Text = T("Bottom Center").Text });
                    f._Alignment.Add(new SelectListItem { Value = "bottomright", Text = T("Bottom Right").Text });

                    f._Scale.Add(new SelectListItem { Value = "downscaleOnly", Text = T("Downscale only").Text });
                    f._Scale.Add(new SelectListItem { Value = "upscaleOnly", Text = T("Upscale only").Text });
                    f._Scale.Add(new SelectListItem { Value = "both", Text = T("Both").Text });
                    f._Scale.Add(new SelectListItem { Value = "upscaleCanvas", Text = T("Upscale canvas").Text });

                    return f;
                };

            context.Form("ResizeFilter", form);
        }
    }
}