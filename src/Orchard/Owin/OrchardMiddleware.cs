using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Orchard.Mvc.Routes;

namespace Orchard.Owin
{
    /// <summary>
    /// A special middleware that is executed last in the tenant pipeline and runs the routed (MVC) part of the request.
    /// </summary>
    public static class OrchardMiddleware
    {
        public static IApplicationBuilder UseOrchard(this IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var handler = context.Items[ShellRoute.OrchardHandlerKey] as RequestDelegate;

                if (handler == null)
                {
                    throw new ArgumentException("orchard.Handler can't be null");
                }

                await handler(context);
            });

            return app;
        }
    }
}
