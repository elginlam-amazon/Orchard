using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.Core.Containers.Models;

namespace Orchard.Core.Containers.ViewModels
{
    public class ContainerWidgetViewModel
    {
        public bool UseFilter { get; set; }
        public SelectList AvailableContainers { get; set; }
        public ContainerWidgetPart Part { get; set; }
    }
}