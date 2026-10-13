using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Security;

namespace Orchard.Users.Services
{
    public class DefaultUserManagementActionsProvider : IUserManagementActionsProvider
    {
        public IEnumerable<Func<IHtmlHelper, IHtmlContent>> UserActionLinks(IUser user)
        {
            yield break;
        }
    }
}