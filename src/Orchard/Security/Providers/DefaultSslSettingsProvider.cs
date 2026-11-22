namespace Orchard.Security.Providers
{
    public class DefaultSslSettingsProvider : ISslSettingsProvider
    {
        public bool RequireSSL { get; set; }

        public DefaultSslSettingsProvider()
        {
            RequireSSL = false;
        }

        public bool GetRequiresSSL()
        {
            return RequireSSL;
        }
    }
}
