// Stubs for legacy Azure SDK types not available in net10
using System;

namespace Microsoft.Azure
{
    public static class CloudConfigurationManager
    {
        public static string GetSetting(string name)
            => Environment.GetEnvironmentVariable(name);
    }
}
