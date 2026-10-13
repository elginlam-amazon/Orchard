using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.DisplayManagement.Shapes;
using Orchard.Environment.Configuration;
using Orchard.Localization;
using Orchard.Mvc.Html;
using Orchard.Mvc.Spooling;
using Orchard.Security;
using Orchard.Security.Permissions;
using Orchard.UI.Resources;

namespace Orchard.Mvc.ViewEngines.Razor
{

    /// <summary>
    /// Base class of the Orchard Razor views (configured with <c>@inherits</c> in the _ViewImports.cshtml file).
    /// </summary>
    public abstract class WebViewPage<TModel> : RazorPage<TModel>, IOrchardViewPage, IViewDataContainer
    {
        private ScriptRegister _scriptRegister;
        private ResourceRegister _stylesheetRegister;
        private Localizer _localizer = NullLocalizer.Instance;
        private object _display;
        private WorkContext _workContext;

        /// <summary>
        /// The app relative virtual path of the view (~/...).
        /// </summary>
        public string VirtualPath => Path == null ? null : (Path.StartsWith("/") ? "~" + Path : Path);

        ViewDataDictionary IViewDataContainer.ViewData
        {
            get { return ViewData; }
            set { ViewData = (ViewDataDictionary<TModel>)value; }
        }

        public Localizer T
        {
            get
            {
                // first time used, create it
                if (_localizer == NullLocalizer.Instance)
                {

                    // if the Model is a shape, get localization scopes from binding sources
                    // e.g., Logon.cshtml in a theme, overriging Users/Logon.cshtml, needs T to 
                    // fallback to the one in Users
                    var shape = Model as IShape;
                    if (shape != null && shape.Metadata.BindingSources.Count > 1)
                    {
                        var localizers = shape.Metadata.BindingSources.Reverse().Select(scope => LocalizationUtilities.Resolve(ViewContext, scope)).ToList();
                        _localizer = (text, args) =>
                        {
                            foreach (var localizer in localizers)
                            {
                                var hint = localizer(text, args);
                                // if not localized using this scope, use next scope
                                if (hint.Text != text)
                                {
                                    return hint;
                                }
                            }

                            // no localization found, return default value
                            return new LocalizedString(text, VirtualPath, text, args);
                        };
                    }
                    else
                    {
                        // not a shape, use the VirtualPath as scope
                        _localizer = LocalizationUtilities.Resolve(ViewContext, VirtualPath);
                    }
                }

                return _localizer;
            }
        }

        public dynamic Display => _display ?? (_display = DisplayHelperFactory.CreateHelper(ViewContext, this));

        // The Orchard layout is a shape, the Razor (string) layout is not used.
        public new dynamic Layout => WorkContext.Layout;

        public WorkContext WorkContext
        {
            get { return _workContext ?? (_workContext = ViewContext.GetWorkContext()); }
            set { _workContext = value; }
        }

        public dynamic New => ShapeFactory;

        private IDisplayHelperFactory _displayHelperFactory;
        public IDisplayHelperFactory DisplayHelperFactory => _displayHelperFactory ?? (_displayHelperFactory = WorkContext.Resolve<IDisplayHelperFactory>());

        private IShapeFactory _shapeFactory;
        public IShapeFactory ShapeFactory => _shapeFactory ?? (_shapeFactory = WorkContext.Resolve<IShapeFactory>());

        private IAuthorizer _authorizer;
        public IAuthorizer Authorizer => _authorizer ?? (_authorizer = WorkContext.Resolve<IAuthorizer>());

        private IContentManager _contentManager;
        public dynamic BuildDisplay(IContent content, string displayType = "", string groupId = "")
        {
            if (_contentManager == null)
            {
                _contentManager = WorkContext.Resolve<IContentManager>();
            }

            return _contentManager.BuildDisplay(content, displayType, groupId);
        }

        public ScriptRegister Script
        {
            get
            {
                return _scriptRegister ??
                    (_scriptRegister = new WebViewScriptRegister(this, ResourceManager));
            }
        }

        private IResourceManager _resourceManager;
        public IResourceManager ResourceManager => _resourceManager ?? (_resourceManager = WorkContext.Resolve<IResourceManager>());

        public ResourceRegister Style
        {
            get
            {
                return _stylesheetRegister ??
                    (_stylesheetRegister = new ResourceRegister(this, ResourceManager, "stylesheet"));
            }
        }

        private string[] _commonLocations;
        public string[] CommonLocations => _commonLocations ?? (_commonLocations = WorkContext.Resolve<ExtensionLocations>().CommonLocations);

        public void RegisterImageSet(string imageSet, string style = "", int size = 16)
        {
            // hack to fake the style "alternate" for now so we don't have to change stylesheet names when this is hooked up
            // todo: (heskew) deal in shapes so we have real alternates 
            var imageSetStylesheet = !string.IsNullOrWhiteSpace(style)
                ? string.Format("{0}-{1}.css", imageSet, style)
                : string.Format("{0}.css", imageSet);
            Style.Include(imageSetStylesheet);
        }

        public virtual void RegisterLink(LinkEntry link)
        {
            ResourceManager.RegisterLink(link);
        }

        public void SetMeta(string name = null, string content = null, string httpEquiv = null, string charset = null)
        {
            var metaEntry = new MetaEntry(name, content, httpEquiv, charset);
            SetMeta(metaEntry);
        }

        public virtual void SetMeta(MetaEntry meta)
        {
            ResourceManager.SetMeta(meta);
        }

        public void AppendMeta(string name, string content, string contentSeparator)
        {
            AppendMeta(new MetaEntry { Name = name, Content = content }, contentSeparator);
        }

        public virtual void AppendMeta(MetaEntry meta, string contentSeparator)
        {
            ResourceManager.AppendMeta(meta, contentSeparator);
        }

        public bool AuthorizedFor(Permission permission)
        {
            return Authorizer.Authorize(permission);
        }

        public bool AuthorizedFor(Permission permission, IContent content)
        {
            return Authorizer.Authorize(permission, content);
        }

        public bool HasText(object thing)
        {
            return !string.IsNullOrWhiteSpace(Convert.ToString(thing));
        }

        public OrchardTagBuilder Tag(dynamic shape, string tagName)
        {
            return WorkContext.Resolve<ITagBuilderFactory>().Create(shape, tagName);
        }

        public IHtmlContent DisplayChildren(dynamic shape)
        {
            var writer = new HtmlStringWriter();
            foreach (var item in shape)
            {
                IHtmlContent content = Display(item);
                content?.WriteTo(writer, HtmlEncoder.Default);
            }
            return writer;
        }

        /// <summary>
        /// Resolves an application url (~/...), optionally appending url encoded path segments.
        /// </summary>
        /// <remarks>
        /// The tenant url prefix is part of the request path base, so it is added by the url helper.
        /// </remarks>
        public string Href(string path, params object[] pathParts)
        {
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var url = base.Href(path);
            if (pathParts != null && pathParts.Length > 0)
            {
                url = url.TrimEnd('/') + "/" + string.Join("/", pathParts.Select(p => Uri.EscapeDataString(Convert.ToString(p))));
            }

            return url;
        }

        internal void PushCaptureWriter(TextWriter writer)
        {
            PushWriter(writer);
        }

        internal void PopCaptureWriter()
        {
            PopWriter();
        }

        public IDisposable Capture(Action<IHtmlContent> callback)
        {
            return new CaptureScope(this, callback);
        }

        public IDisposable Capture(dynamic zone, string position = null)
        {
            return new CaptureScope(this, html => zone.Add(html, position));
        }

        class CaptureScope : IDisposable
        {
            readonly WebViewPage<TModel> _viewPage;
            readonly Action<IHtmlContent> _callback;
            readonly HtmlStringWriter _writer = new HtmlStringWriter();

            public CaptureScope(WebViewPage<TModel> viewPage, Action<IHtmlContent> callback)
            {
                _viewPage = viewPage;
                _callback = callback;
                _viewPage.PushCaptureWriter(_writer);
            }

            void IDisposable.Dispose()
            {
                _viewPage.PopCaptureWriter();
                _callback(_writer);
            }
        }

        class WebViewScriptRegister : ScriptRegister
        {
            private readonly WebViewPage<TModel> _viewPage;

            public WebViewScriptRegister(WebViewPage<TModel> viewPage, IResourceManager resourceManager)
                : base(viewPage, resourceManager)
            {
                _viewPage = viewPage;
            }

            public override IDisposable Head()
            {
                return new CaptureScope(_viewPage, s => ResourceManager.RegisterHeadScript(s.ToString()));
            }

            public override IDisposable Foot()
            {
                return new CaptureScope(_viewPage, s => ResourceManager.RegisterFootScript(s.ToString()));
            }
        }
    }

    public abstract class WebViewPage : WebViewPage<dynamic>
    {
    }
}
