using System;
using System.Configuration;

namespace Orchard.Azure.Services.Environment.Configuration
{

    /// <summary>
    /// Provides a default <c>IPlatformConfigurationAccessor</c> implementation that reads configuration settings
    /// from cloud service role configuration, app settings and connection strings.
    /// </summary>
    /// <remarks>
    /// Settings are read first from the environment variables (which is how Microsoft Azure App Service exposes its
    /// app settings), secondly in the app settings of the application configuration file (the former
    /// <c>CloudConfigurationManager</c> behavior, cloud service role configuration is not supported on .NET).
    /// If the setting is not found, the connection strings of the application configuration file are checked. Both the tenant-specific name and the tenant-neutral name are checked within each configuration source
    /// before proceeding to the next one.
    /// </remarks>
    public class DefaultPlatformConfigurationAccessor : IPlatformConfigurationAccessor
    {

        public string GetSetting(string name, string tenant, string namePrefix = null)
        {
            var tenantName = string.Format("{0}:{1}{2}", tenant, namePrefix, name);
            var fallbackName = string.Format("{0}{1}", namePrefix, name);

            var cloudConfigurationValue = GetPlatformSetting(tenantName) ?? GetPlatformSetting(fallbackName);
            if (!string.IsNullOrEmpty(cloudConfigurationValue))
                return cloudConfigurationValue;

            var connectionStringValue = ConfigurationManager.ConnectionStrings[tenantName] ?? ConfigurationManager.ConnectionStrings[fallbackName];
            if (connectionStringValue != null)
                return connectionStringValue.ConnectionString;

            return null;
        }

        /// <summary>
        /// Reads a setting from the environment variables or the app settings (the former CloudConfigurationManager.GetSetting).
        /// </summary>
        public static string GetPlatformSetting(string name)
        {
            var value = System.Environment.GetEnvironmentVariable(name)
                ?? System.Environment.GetEnvironmentVariable("APPSETTING_" + name)
                ?? ConfigurationManager.AppSettings[name];

            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}