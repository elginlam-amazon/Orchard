namespace Orchard.Templates.Compilation.Razor
{
    /// <summary>
    /// Base class of the Razor templates (an Orchard view page).
    /// </summary>
    public abstract class RazorTemplateBase<T> : Mvc.ViewEngines.Razor.WebViewPage<T>
    {
    }
}
