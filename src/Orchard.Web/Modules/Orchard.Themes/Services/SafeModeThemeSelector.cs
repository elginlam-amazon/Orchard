using Microsoft.AspNetCore.Mvc;

namespace Orchard.Themes.Services
{
    public class SafeModeThemeSelector : IThemeSelector
    {
        public ThemeSelectorResult GetTheme(ActionContext context)
        {
            return new ThemeSelectorResult { Priority = -100, ThemeName = "Themes" };
        }
    }
}