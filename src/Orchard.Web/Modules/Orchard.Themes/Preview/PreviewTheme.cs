using System;
using Orchard.Mvc;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Orchard.Themes.Preview
{
    public class PreviewTheme : IPreviewTheme
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly string PreviewThemeKey = typeof(PreviewTheme).FullName;

        public PreviewTheme(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetPreviewTheme()
        {
            var session = _httpContextAccessor.Current()?.Features.Get<ISessionFeature>()?.Session;
            if (session != null)
            {
                return session.GetString(PreviewThemeKey);
            }

            return null;
        }

        public void SetPreviewTheme(string themeName)
        {
            var session = _httpContextAccessor.Current()?.Features.Get<ISessionFeature>()?.Session;
            if (session != null)
            {
                if (string.IsNullOrEmpty(themeName))
                {
                    session.Remove(PreviewThemeKey);
                }
                else
                {
                    session.SetString(PreviewThemeKey, themeName);
                }
            }
        }
    }
}