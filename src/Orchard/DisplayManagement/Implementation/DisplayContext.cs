using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Mvc;

namespace Orchard.DisplayManagement.Implementation
{
    public class DisplayContext
    {
        public DisplayHelper Display { get; set; }
        public ViewContext ViewContext { get; set; }
        public IViewDataContainer ViewDataContainer { get; set; }
        public object Value { get; set; }
    }
}
