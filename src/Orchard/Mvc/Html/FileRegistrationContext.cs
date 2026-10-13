using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Orchard.Mvc.Html
{
    public class FileRegistrationContext : ActionContext
    {
        private readonly TagRenderMode _fileTagRenderMode;
        private readonly TagBuilder _tagBuilder;
        private static readonly Dictionary<string, string> _filePathAttributes = new Dictionary<string, string> { { "script", "src" }, { "link", "href" } };
        private static readonly Dictionary<string, TagRenderMode> _fileTagRenderModes = new Dictionary<string, TagRenderMode> { { "script", TagRenderMode.Normal }, { "link", TagRenderMode.SelfClosing } };

        public FileRegistrationContext(ControllerContext viewContext, IViewDataContainer viewDataContainer, string tagName, string fileName, string position)
            : this(viewContext, viewDataContainer, tagName, fileName, position, _filePathAttributes[tagName], _fileTagRenderModes[tagName])
        {
        }

        public FileRegistrationContext(ControllerContext viewContext, IViewDataContainer viewDataContainer, string tagName, string fileName, string position, string filePathAttributeName, TagRenderMode fileTagRenderMode)
            : base(viewContext.HttpContext, viewContext.RouteData, viewContext.ActionDescriptor)
        {
            _fileTagRenderMode = fileTagRenderMode;
            Container = viewDataContainer as IRazorPage;

            if (Container != null && Container.Path != null)
            {
                var containerPath = Container.Path.StartsWith("/") ? "~" + Container.Path : Container.Path;
                var viewsIndex = containerPath.IndexOf("/Views", StringComparison.InvariantCultureIgnoreCase);
                ContainerVirtualPath = viewsIndex >= 0 ? containerPath.Substring(0, viewsIndex) : containerPath;
            }

            FileName = fileName;
            Position = position;
            FilePathAttributeName = filePathAttributeName;
            _tagBuilder = new TagBuilder(tagName);
        }

        public IRazorPage Container { get; set; }
        public string ContainerVirtualPath { get; set; }
        public string FileName { get; set; }
        public string FilePathAttributeName { get; private set; }
        public string Condition { get; set; }
        public string Position { get; set; }

        public void AddAttribute(string name, string value)
        {
            _tagBuilder.MergeAttribute(name, value);
        }

        public void SetAttribute(string name, string value)
        {
            _tagBuilder.MergeAttribute(name, value, true);
        }

        internal string GetFilePath(string containerRelativePath)
        {
            //todo: (heskew) maybe not here but file paths for non-app locations need to be taken into account
            var path = ContainerVirtualPath + containerRelativePath + FileName;
            return Container != null && path.StartsWith("~/")
                       ? HttpContext.Request.PathBase + path.Substring(1)
                       : path;
        }

        internal string GetTag()
        {
            return _tagBuilder.ToString(_fileTagRenderMode);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj.GetType() == typeof(FileRegistrationContext) && Equals((FileRegistrationContext)obj);
        }

        private bool Equals(FileRegistrationContext other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(other.ContainerVirtualPath, ContainerVirtualPath) && Equals(other.FileName, FileName) && Equals(other.Condition, Condition);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = ContainerVirtualPath != null ? ContainerVirtualPath.GetHashCode() : 0;
                result = (result * 397) ^ (FileName != null ? FileName.GetHashCode() : 0);
                result = (result * 397) ^ (Condition != null ? Condition.GetHashCode() : 0);
                return result;
            }
        }
    }
}