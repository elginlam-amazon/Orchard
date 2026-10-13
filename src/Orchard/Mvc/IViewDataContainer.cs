using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Orchard.Mvc
{
    /// <summary>
    /// An object holding view data (formerly System.Web.Mvc.IViewDataContainer), e.g. a view page.
    /// </summary>
    public interface IViewDataContainer
    {
        ViewDataDictionary ViewData { get; set; }
    }

    /// <summary>
    /// Simple <see cref="IViewDataContainer"/> implementation.
    /// </summary>
    public class ViewDataContainer : IViewDataContainer
    {
        public ViewDataContainer(ViewDataDictionary viewData)
        {
            ViewData = viewData;
        }

        public ViewDataDictionary ViewData { get; set; }
    }
}
