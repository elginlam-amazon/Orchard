using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Orchard.Caching;
using Orchard.Data;
using Orchard.Environment.AutofacUtil;
using Orchard.Environment.Configuration;
using Orchard.Environment.Descriptor;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Compilers;
using Orchard.Environment.Extensions.Folders;
using Orchard.Environment.Extensions.Loaders;
using Orchard.Environment.ShellBuilders;
using Orchard.Environment.State;
using Orchard.Events;
using Orchard.Exceptions;
using Orchard.FileSystems.AppData;
using Orchard.FileSystems.Dependencies;
using Orchard.FileSystems.LockFile;
using Orchard.FileSystems.VirtualPath;
using Orchard.FileSystems.WebSite;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Services;
using Orchard.UI.Resources;

namespace Orchard.Environment
{
    public static class OrchardStarter
    {
        public static IContainer CreateHostContainer(Action<ContainerBuilder> registrations)
        {
            ExtensionLocations extensionLocations = new ExtensionLocations();

            var builder = new ContainerBuilder();
            // Application paths and parameters
            builder.RegisterInstance(extensionLocations);

            builder.RegisterModule(new LoggingModule());
            builder.RegisterModule(new EventsModule());
            builder.RegisterModule(new CacheModule());

            // a single default host implementation is needed for bootstrapping a web app domain
            builder.RegisterType<DefaultOrchardEventBus>().As<IEventBus>().SingleInstance();
            builder.RegisterType<DefaultCacheHolder>().As<ICacheHolder>().SingleInstance();
            builder.RegisterType<DefaultCacheContextAccessor>().As<ICacheContextAccessor>().SingleInstance();
            builder.RegisterType<DefaultParallelCacheContext>().As<IParallelCacheContext>().SingleInstance();
            builder.RegisterType<DefaultAsyncTokenProvider>().As<IAsyncTokenProvider>().SingleInstance();
            builder.RegisterType<DefaultHostEnvironment>().As<IHostEnvironment>().SingleInstance();
            builder.RegisterType<DefaultHostLocalRestart>().As<IHostLocalRestart>().Named<IEventHandler>(typeof(IShellSettingsManagerEventHandler).Name).SingleInstance();
            builder.RegisterType<DefaultBuildManager>().As<IBuildManager>().SingleInstance();
            builder.RegisterType<AppDataFolderRoot>().As<IAppDataFolderRoot>().SingleInstance();
            builder.RegisterType<DefaultExtensionCompiler>().As<IExtensionCompiler>().SingleInstance();
            builder.RegisterType<DefaultProjectFileParser>().As<IProjectFileParser>().SingleInstance();
            builder.RegisterType<DefaultAssemblyLoader>().As<IAssemblyLoader>().SingleInstance();
            builder.RegisterType<AppDomainAssemblyNameResolver>().As<IAssemblyNameResolver>().SingleInstance();
            builder.RegisterType<GacAssemblyNameResolver>().As<IAssemblyNameResolver>().SingleInstance();
            builder.RegisterType<OrchardFrameworkAssemblyNameResolver>().As<IAssemblyNameResolver>().SingleInstance();
            builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().InstancePerDependency();
            builder.RegisterType<ViewsBackgroundCompilation>().As<IViewsBackgroundCompilation>().SingleInstance();
            builder.RegisterType<DefaultExceptionPolicy>().As<IExceptionPolicy>().SingleInstance();
            builder.RegisterType<DefaultCriticalErrorProvider>().As<ICriticalErrorProvider>().SingleInstance();
            builder.RegisterType<ResourceFileHashProvider>().As<IResourceFileHashProvider>().SingleInstance();
            //builder.RegisterType<RazorTemplateCache>().As<IRazorTemplateProvider>().SingleInstance();

            RegisterVolatileProvider<WebSiteFolder, IWebSiteFolder>(builder);
            RegisterVolatileProvider<AppDataFolder, IAppDataFolder>(builder);
            RegisterVolatileProvider<DefaultLockFileManager, ILockFileManager>(builder);
            RegisterVolatileProvider<Clock, IClock>(builder);
            RegisterVolatileProvider<DefaultDependenciesFolder, IDependenciesFolder>(builder);
            RegisterVolatileProvider<DefaultExtensionDependenciesManager, IExtensionDependenciesManager>(builder);
            RegisterVolatileProvider<DefaultAssemblyProbingFolder, IAssemblyProbingFolder>(builder);
            RegisterVolatileProvider<DefaultVirtualPathMonitor, IVirtualPathMonitor>(builder);
            RegisterVolatileProvider<DefaultVirtualPathProvider, IVirtualPathProvider>(builder);

            builder.RegisterType<DefaultOrchardHost>().As<IOrchardHost>().As<IEventHandler>()
                .Named<IEventHandler>(typeof(IShellSettingsManagerEventHandler).Name)
                .Named<IEventHandler>(typeof(IShellDescriptorManagerEventHandler).Name)
                .SingleInstance();
            {
                builder.RegisterType<ShellSettingsManager>().As<IShellSettingsManager>().SingleInstance();

                builder.RegisterType<ShellContextFactory>().As<IShellContextFactory>().SingleInstance();
                {
                    builder.RegisterType<ShellDescriptorCache>().As<IShellDescriptorCache>().SingleInstance();

                    builder.RegisterType<CompositionStrategy>().As<ICompositionStrategy>().SingleInstance();
                    {
                        builder.RegisterType<ShellContainerRegistrations>().As<IShellContainerRegistrations>().SingleInstance();
                        builder.RegisterType<ExtensionLoaderCoordinator>().As<IExtensionLoaderCoordinator>().SingleInstance();
                        builder.RegisterType<ExtensionMonitoringCoordinator>().As<IExtensionMonitoringCoordinator>().SingleInstance();
                        builder.RegisterType<ExtensionManager>().As<IExtensionManager>().SingleInstance();
                        {
                            builder.RegisterType<ExtensionHarvester>().As<IExtensionHarvester>().SingleInstance();
                            builder.RegisterType<ModuleFolders>().As<IExtensionFolders>().SingleInstance()
                                .WithParameter(new NamedParameter("paths", extensionLocations.ModuleLocations));
                            builder.RegisterType<CoreModuleFolders>().As<IExtensionFolders>().SingleInstance()
                                .WithParameter(new NamedParameter("paths", extensionLocations.CoreLocations));
                            builder.RegisterType<ThemeFolders>().As<IExtensionFolders>().SingleInstance()
                                .WithParameter(new NamedParameter("paths", extensionLocations.ThemeLocations));

                            builder.RegisterType<CoreExtensionLoader>().As<IExtensionLoader>().SingleInstance();
                            builder.RegisterType<ReferencedExtensionLoader>().As<IExtensionLoader>().SingleInstance();
                            builder.RegisterType<PrecompiledExtensionLoader>().As<IExtensionLoader>().SingleInstance();
                            builder.RegisterType<DynamicExtensionLoader>().As<IExtensionLoader>().SingleInstance();
                            builder.RegisterType<RawThemeExtensionLoader>().As<IExtensionLoader>().SingleInstance();
                        }
                    }

                    builder.RegisterType<ShellContainerFactory>().As<IShellContainerFactory>().SingleInstance();
                }

                builder.RegisterType<DefaultProcessingEngine>().As<IProcessingEngine>().SingleInstance();
            }

            builder.RegisterType<RunningShellTable>().As<IRunningShellTable>().SingleInstance();
            builder.RegisterType<DefaultOrchardShell>().As<IOrchardShell>().InstancePerMatchingLifetimeScope("shell");
            builder.RegisterType<SessionConfigurationCache>().As<ISessionConfigurationCache>().InstancePerMatchingLifetimeScope("shell");

            registrations(builder);

            var optionalHostConfig = HostingEnvironment.MapPath("~/Config/Host.config");
            if (File.Exists(optionalHostConfig))
                builder.RegisterModule(new ConfigurationSettingsReader(ConfigurationSettingsReaderConstants.DefaultSectionName, optionalHostConfig));

            var optionalComponentsConfig = HostingEnvironment.MapPath("~/Config/HostComponents.config");
            if (File.Exists(optionalComponentsConfig))
                builder.RegisterModule(new HostComponentsConfigModule(optionalComponentsConfig));

            var container = builder.Build();

            // Note: the MVC integration (controller activation, filters, view engines, model validation)
            // is configured by the ASP.NET Core host, see OrchardMvcBuilderExtensions.

            var hostContainer = new DefaultOrchardHostContainer(container);
            //MvcServiceLocator.SetCurrent(hostContainer);
            OrchardHostContainerRegistry.RegisterHostContainer(hostContainer);

            return container;
        }

        private static void RegisterVolatileProvider<TRegister, TService>(ContainerBuilder builder) where TService : IVolatileProvider
        {
            builder.RegisterType<TRegister>()
                .As<TService>()
                .As<IVolatileProvider>()
                .SingleInstance();
        }

        public static IOrchardHost CreateHost(Action<ContainerBuilder> registrations)
        {
            var container = CreateHostContainer(registrations);
            return container.Resolve<IOrchardHost>();
        }
    }
}
