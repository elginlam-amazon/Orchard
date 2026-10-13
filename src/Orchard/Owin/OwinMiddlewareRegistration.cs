using System;
using Microsoft.AspNetCore.Builder;

namespace Orchard.Owin
{
    /// <summary>
    /// A middleware registration that can make changes to the tenant request pipeline, like registering middlewares to be injected into the Orchard 
    /// pipeline.
    /// </summary>
    public class OwinMiddlewareRegistration
    {
        /// <summary>
        /// Gets or sets the delegate that you can use to make changes to the tenant pipeline, like registering middlewares to be injected into
        /// the Orchard pipeline.
        /// </summary>
        public Action<IApplicationBuilder> Configure { get; set; }

        /// <summary>
        /// Gets or sets the priority value that decides the order in which such objects are processed. I.e. "0" will run before "10", 
        /// but registrations without a priority value will run before the ones that have it set.
        /// Note that this priority notation is the same as the one for shape placement (so you can e.g. use ":before").
        /// </summary>
        public string Priority { get; set; }
    }
}
