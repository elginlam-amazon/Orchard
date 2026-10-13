using Microsoft.AspNetCore.Mvc;
using Orchard.Environment.Extensions.Models;

namespace Orchard.Themes
{
    public interface IThemeManager : IDependency
    {
        ExtensionDescriptor GetRequestTheme(ActionContext requestContext);
    }
}
