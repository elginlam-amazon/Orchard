using System;
using System.Linq;
using Autofac;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Web;

namespace Orchard.Specs.Hosting.Orchard.Web
{
    /// <summary>
    /// Gives access to the Orchard host of the site under test (formerly the Global.asax of the site template,
    /// running in the ASP.NET AppDomain of the site).
    /// </summary>
    public static class MvcApplication
    {
        /// <summary>
        /// The running Orchard site.
        /// </summary>
        internal static OrchardApplication Application { get; set; }

        /// <summary>
        /// The host container of the running Orchard site (available once the site has served a request).
        /// </summary>
        private static IContainer Container => Application?.HostContainer
            ?? throw new InvalidOperationException("The Orchard site is not initialized.");

        private static IOrchardHost Host => Container.Resolve<IOrchardHost>();

        public static void ReloadExtensions()
        {
            Host.ReloadExtensions();
        }

        public static void RestartTenant(string name)
        {
            var settings = Container.Resolve<IShellSettingsManager>().LoadSettings().SingleOrDefault(x => x.Name == name);
            if (settings == null)
            {
                settings = new ShellSettings
                {
                    Name = name,
                    State = TenantState.Uninitialized
                };
            }

            ((DefaultOrchardHost)Host).ActivateShell(settings);
        }

        public static IWorkContextScope CreateStandaloneEnvironment(string name)
        {
            var settings = Container.Resolve<IShellSettingsManager>().LoadSettings().SingleOrDefault(x => x.Name == name);
            if (settings == null)
            {
                settings = new ShellSettings
                {
                    Name = name,
                    State = TenantState.Uninitialized
                };
            }

            return Host.CreateStandaloneEnvironment(settings);
        }
    }
}
