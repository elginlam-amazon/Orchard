using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Security;

namespace Orchard.Users.Services
{
    public interface IUserManagementActionsProvider : IDependency
    {
        // Using a delegate so implementations don't have to build/figure out
        // their own IHtmlHelper
        IEnumerable<Func<IHtmlHelper, IHtmlContent>> UserActionLinks(IUser user);
    }
}
