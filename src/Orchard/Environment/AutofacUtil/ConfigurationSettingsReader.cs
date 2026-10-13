using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Autofac;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

namespace Orchard.Environment.AutofacUtil
{
    /// <summary>
    /// Reads the legacy Autofac XML configuration files (~/Config/Host.config, ~/Config/Sites.config...)
    /// and registers their components and modules.
    /// </summary>
    /// <remarks>
    /// Autofac.Configuration 4+ only reads Microsoft.Extensions.Configuration sources, with a different schema.
    /// This module converts the legacy &lt;autofac&gt; section schema (components/component, modules/module,
    /// parameters, properties) into the new configuration keys, so that the existing configuration files keep working.
    /// </remarks>
    internal class ConfigurationSettingsReader : Module
    {
        private readonly string _sectionName;
        private readonly string _configurationFile;

        public ConfigurationSettingsReader(string sectionName, string configurationFile)
        {
            _sectionName = sectionName ?? ConfigurationSettingsReaderConstants.DefaultSectionName;
            _configurationFile = configurationFile;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var settings = ReadSettings(XDocument.Load(_configurationFile), _sectionName);

            if (!settings.Any())
            {
                return;
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            builder.RegisterModule(new ConfigurationModule(configuration));
        }

        public static IDictionary<string, string> ReadSettings(XDocument document, string sectionName)
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var section = document.Root?.Name.LocalName == sectionName
                ? document.Root
                : document.Root?.Element(sectionName);

            if (section == null)
            {
                return settings;
            }

            var defaultAssembly = (string)section.Attribute("defaultAssembly");
            if (!string.IsNullOrEmpty(defaultAssembly))
            {
                settings["defaultAssembly"] = defaultAssembly;
            }

            var index = 0;
            foreach (var component in section.Elements("components").Elements("component"))
            {
                var prefix = "components:" + index++ + ":";
                settings[prefix + "type"] = (string)component.Attribute("type");

                var serviceIndex = 0;
                var service = (string)component.Attribute("service");
                if (!string.IsNullOrEmpty(service))
                {
                    settings[prefix + "services:" + serviceIndex++ + ":type"] = service;
                }

                foreach (var additionalService in component.Elements("services").Elements("service"))
                {
                    settings[prefix + "services:" + serviceIndex++ + ":type"] = (string)additionalService.Attribute("type");
                }

                CopyAttribute(component, "instance-scope", settings, prefix + "instanceScope");
                CopyAttribute(component, "inject-properties", settings, prefix + "injectProperties");
                CopyAttribute(component, "auto-activate", settings, prefix + "autoActivate");
                CopyAttribute(component, "ownership", settings, prefix + "ownership");

                CopyNameValues(component.Elements("parameters").Elements("parameter"), settings, prefix + "parameters:");
                CopyNameValues(component.Elements("properties").Elements("property"), settings, prefix + "properties:");
            }

            index = 0;
            foreach (var module in section.Elements("modules").Elements("module"))
            {
                var prefix = "modules:" + index++ + ":";
                settings[prefix + "type"] = (string)module.Attribute("type");
                CopyNameValues(module.Elements("parameters").Elements("parameter"), settings, prefix + "parameters:");
                CopyNameValues(module.Elements("properties").Elements("property"), settings, prefix + "properties:");
            }

            return settings;
        }

        private static void CopyAttribute(XElement element, string attributeName, IDictionary<string, string> settings, string key)
        {
            var value = (string)element.Attribute(attributeName);
            if (!string.IsNullOrEmpty(value))
            {
                settings[key] = value;
            }
        }

        private static void CopyNameValues(IEnumerable<XElement> elements, IDictionary<string, string> settings, string prefix)
        {
            foreach (var element in elements)
            {
                var name = (string)element.Attribute("name");
                if (!string.IsNullOrEmpty(name))
                {
                    settings[prefix + name] = (string)element.Attribute("value");
                }
            }
        }
    }
}
