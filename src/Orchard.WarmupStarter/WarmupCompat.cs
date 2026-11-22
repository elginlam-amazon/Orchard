using System;
using System.Web;

namespace Orchard.WarmupStarter
{
    internal delegate IAsyncResult BeginEventHandler(object sender, EventArgs e, AsyncCallback cb, object extraData);
    internal delegate void EndEventHandler(IAsyncResult ar);

    internal static class HttpApplicationExtensions
    {
        internal static void AddOnBeginRequestAsync(this HttpApplication app, BeginEventHandler beg, EndEventHandler end, object extraData)
        {
            // APM async pipeline hook - no-op stub for net10 migration
        }
    }
}
