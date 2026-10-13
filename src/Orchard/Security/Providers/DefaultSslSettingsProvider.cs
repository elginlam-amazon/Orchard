namespace Orchard.Security.Providers
{
    public class DefaultSslSettingsProvider : ISslSettingsProvider
    {
        public bool RequireSSL { get; set; }

        public DefaultSslSettingsProvider()
        {
            // formerly the forms authentication requireSSL setting, which is off by default.
            RequireSSL = false;
        }

        public bool GetRequiresSSL()
        {
            return RequireSSL;
        }
    }
}
