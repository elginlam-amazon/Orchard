using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Xml.Linq;

namespace Orchard.Email.Services
{
    /// <summary>
    /// The SMTP settings of the &lt;system.net&gt;&lt;mailSettings&gt;&lt;smtp&gt; configuration element of the application
    /// configuration file (System.Net.Configuration.SmtpSection is not available on .NET).
    /// </summary>
    public class SmtpSection
    {
        public string From { get; set; }
        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;
        public SmtpNetworkElement Network { get; set; } = new SmtpNetworkElement();

        /// <summary>
        /// Reads the SMTP settings of the application configuration file, or returns <c>null</c> if none is defined.
        /// </summary>
        public static SmtpSection Load()
        {
            // the application configuration file (Orchard.Web.dll.config, built from the host App.config)
            var configurationFile = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).FilePath;
            if (configurationFile == null || !File.Exists(configurationFile))
            {
                return null;
            }

            var smtp = XDocument.Load(configurationFile).Root?
                .Element("system.net")?
                .Element("mailSettings")?
                .Element("smtp");

            if (smtp == null)
            {
                return null;
            }

            var section = new SmtpSection
            {
                From = (string)smtp.Attribute("from"),
            };

            if (Enum.TryParse((string)smtp.Attribute("deliveryMethod"), true, out SmtpDeliveryMethod deliveryMethod))
            {
                section.DeliveryMethod = deliveryMethod;
            }

            var network = smtp.Element("network");
            if (network != null)
            {
                section.Network.Host = (string)network.Attribute("host");
                section.Network.Port = int.TryParse((string)network.Attribute("port"), out var port) ? port : 25;
                section.Network.EnableSsl = bool.TryParse((string)network.Attribute("enableSsl"), out var enableSsl) && enableSsl;
                section.Network.DefaultCredentials = bool.TryParse((string)network.Attribute("defaultCredentials"), out var defaultCredentials) && defaultCredentials;
                section.Network.UserName = (string)network.Attribute("userName");
                section.Network.Password = (string)network.Attribute("password");
            }

            return section;
        }
    }

    public class SmtpNetworkElement
    {
        public string Host { get; set; }
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public bool DefaultCredentials { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
