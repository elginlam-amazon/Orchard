using Microsoft.AspNetCore.Mvc;

namespace Orchard.Themes
{
    public class ThemeSelectorResult
    {
        public int Priority { get; set; }
        public string ThemeName { get; set; }
    }

    public interface IThemeSelector : IDependency
    {
        ThemeSelectorResult GetTheme(ActionContext context);
    }

}