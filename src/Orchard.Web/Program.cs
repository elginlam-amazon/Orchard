using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Mvc;
using Orchard.Mvc.DataAnnotations;
using Orchard.Mvc.Filters;
using Orchard.Mvc.ModelBinders;
using Orchard.Mvc.Routes;
using Orchard.Mvc.ViewEngines.Razor;
using Orchard.Mvc.ViewEngines.ThemeAwareness;
using Orchard.Security.Providers;
using Orchard.Utility;
using Orchard.WarmupStarter;
using Orchard.Web.Hosting;
using Orchard.WebApi;
using HostingEnvironment = Orchard.Environment.HostingEnvironment;
using IFilterProvider = Microsoft.AspNetCore.Mvc.Filters.IFilterProvider;

namespace Orchard.Web
{
    /// <summary>
    /// The Orchard web host entry point.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var application = OrchardApplication.Create(WebApplication.CreateBuilder(args));
            application.Start();
            application.App.Run();
        }
    }

    /// <summary>
    /// The Orchard web application (formerly the Global.asax MvcApplication and the Web.config pipeline).
    /// </summary>
    public class OrchardApplication
    {
        // Accept file uploads up to 64 MB.
        private const long MaxRequestLength = 67108864;

        private readonly OrchardRouteCollection _applicationRoutes = new OrchardRouteCollection();
        private readonly ModelBinderDictionary _modelBinders = new ModelBinderDictionary();
        private Starter<IOrchardHost> _starter;

        private OrchardApplication()
        {
        }

        public WebApplication App { get; private set; }

        /// <summary>
        /// The Orchard host container, once the application has been initialized.
        /// </summary>
        public IContainer HostContainer { get; private set; }

        /// <summary>
        /// Configures the services and the request pipeline of the Orchard application.
        /// </summary>
        public static OrchardApplication Create(WebApplicationBuilder builder)
        {
            var application = new OrchardApplication();

            HostingEnvironment.Initialize(builder.Environment.ContentRootPath, () => { });

            application.ConfigureServices(builder);

            var app = builder.Build();
            application.App = app;

            // The tenants stop the application when they need to be restarted (formerly HostingEnvironment.InitiateShutdown).
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            HostingEnvironment.Initialize(builder.Environment.ContentRootPath, lifetime.StopApplication);

            application._starter = new Starter<IOrchardHost>(
                () =>
                {
                    application.HostContainer = application.CreateHostContainer(app.Services);
                    return HostInitialization(application.HostContainer);
                },
                HostBeginRequest,
                HostEndRequest);

            application.Configure(app);

            return application;
        }

        /// <summary>
        /// Starts the initialization of the Orchard host (formerly Application_Start). The requests are held until it completes.
        /// </summary>
        public void Start()
        {
            _starter.OnApplicationStart();
        }

        private void ConfigureServices(WebApplicationBuilder builder)
        {
            var modelBinders = _modelBinders;
            var services = builder.Services;

            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxRequestLength);
            services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MaxRequestLength);

            services.AddHttpContextAccessor();
            services.AddDistributedMemoryCache();
            services.AddSession();
            services.AddAntiforgery();

            // Forms authentication (the tenants register their external authentication schemes at runtime).
            services
                .AddAuthentication(FormsAuthenticationService.AuthenticationScheme)
                .AddCookie(FormsAuthenticationService.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/Users/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(2880);
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

            var mvcBuilder = services
                .AddMvc(options =>
                {
                    // The routes are the Orchard routes published by the tenants.
                    options.EnableEndpointRouting = false;
                    options.ModelBinderProviders.Insert(0, new OrchardModelBinderProvider(modelBinders));
                    options.ModelMetadataDetailsProviders.Add(new LocalizedModelValidatorProvider());
                    options.Conventions.Add(new ControllerAreaConvention());
                    options.Conventions.Add(new ApiControllerConvention());
                })
                .AddRazorRuntimeCompilation(options =>
                {
                    // The Razor templates stored in the database (Orchard.Templates).
                    options.FileProviders.Add(DynamicViewFileProvider.Instance);
                });

            // The controllers of the modules are discovered when the tenants are composed.
            var partManager = mvcBuilder.PartManager;
            ControllerAreaConvention.ControllerAssemblyRegistered += assembly => AddApplicationPart(partManager, assembly);
            foreach (var assembly in ControllerAreaConvention.Assemblies)
            {
                AddApplicationPart(partManager, assembly);
            }

            services.Replace(ServiceDescriptor.Singleton<IControllerActivator, OrchardControllerActivator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IFilterProvider, OrchardFilterProvider>());
            services.AddSingleton<IActionDescriptorChangeProvider>(OrchardActionDescriptorChangeProvider.Instance);
            services.Configure<MvcViewOptions>(options =>
            {
                options.ViewEngines.Clear();
                options.ViewEngines.Add(new ThemeAwareViewEngineShim());
            });
        }

        private static void AddApplicationPart(ApplicationPartManager partManager, Assembly assembly)
        {
            lock (partManager)
            {
                if (partManager.ApplicationParts.OfType<AssemblyPart>().Any(part => part.Assembly == assembly))
                {
                    return;
                }

                partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }

            OrchardActionDescriptorChangeProvider.Instance.NotifyChanged();
        }

        private void Configure(WebApplication app)
        {
            var starter = _starter;
            var applicationRoutes = _applicationRoutes;
            // Hold the requests while the application is warming up.
            app.UseMiddleware<WarmupMiddleware>();

            // Formerly the Orchard OWIN startup.
            app.Use((context, next) =>
            {
                context.Response.Headers.Append("X-Generator", "Orchard");
                return next(context);
            });

            app.UseOrchardStaticFiles();

            app.Use((context, next) =>
            {
                // The application path, used to resolve the application relative paths (~/...).
                context.Items[VirtualPathUtility.ApplicationPathBaseKey] = context.Request.PathBase.Value;
                return next(context);
            });

            // Formerly Application_BeginRequest / Application_EndRequest.
            app.Use((context, next) => starter.InvokeAsync(context, () => next(context)));

            // The url prefix of the tenant is moved into the path base, the tenant routes are relative to it.
            app.Use((context, next) =>
            {
                var shellTable = HostContainer?.ResolveOptional<IRunningShellTable>();
                MoveTenantPrefixToPathBase(context, shellTable?.Match(context));
                return next(context);
            });

            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                // The handler executing the actions matched by the Orchard routes.
                MvcRouteHandler.Inner = routes.DefaultHandler;
                routes.Routes.Add(applicationRoutes);
            });
        }

        private static void MoveTenantPrefixToPathBase(HttpContext context, ShellSettings settings)
        {
            if (settings == null || string.IsNullOrEmpty(settings.RequestUrlPrefix))
            {
                return;
            }

            var prefix = new PathString("/" + settings.RequestUrlPrefix.Trim('/'));
            if (context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var matched, out var remaining))
            {
                context.Request.PathBase = context.Request.PathBase.Add(matched);
                context.Request.Path = remaining;
            }
        }

        private IContainer CreateHostContainer(IServiceProvider applicationServices)
        {
            return OrchardStarter.CreateHostContainer(builder =>
            {
                builder.RegisterInstance(_applicationRoutes).SingleInstance();
                builder.RegisterInstance(_modelBinders).SingleInstance();
                builder.RegisterInstance(applicationServices).Named<IServiceProvider>(MvcModule.ApplicationServicesKey).ExternallyOwned();
                builder.RegisterInstance(applicationServices.GetRequiredService<IApplicationBuilderFactory>()).As<IApplicationBuilderFactory>().ExternallyOwned();
            });
        }

        private static IOrchardHost HostInitialization(IContainer hostContainer)
        {
            var host = hostContainer.Resolve<IOrchardHost>();

            host.Initialize();

            // initialize shells to speed up the first dynamic query
            host.BeginRequest();
            host.EndRequest();

            return host;
        }

        private static void HostBeginRequest(HttpContext context, IOrchardHost host)
        {
            context.Items["originalHttpContext"] = context;
            host.BeginRequest();
        }

        private static void HostEndRequest(HttpContext context, IOrchardHost host)
        {
            host.EndRequest();
        }
    }
}
