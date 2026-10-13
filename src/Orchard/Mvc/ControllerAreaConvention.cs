using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Orchard.Mvc
{
    /// <summary>
    /// Assigns the Orchard area (the extension id) to the controllers discovered by the shells composition,
    /// so that the area based routes published by the tenants can select their actions.
    /// </summary>
    /// <remarks>
    /// The MVC application model is application wide while Orchard extensions are loaded at runtime:
    /// newly discovered controller assemblies are reported through <see cref="ControllerAssemblyRegistered"/>
    /// so that the host can add them to the MVC application parts, and the action descriptors are rebuilt
    /// through <see cref="OrchardActionDescriptorChangeProvider"/>.
    /// </remarks>
    public class ControllerAreaConvention : IControllerModelConvention
    {
        private static readonly ConcurrentDictionary<Type, string> _areas = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<Assembly, bool> _assemblies = new ConcurrentDictionary<Assembly, bool>();

        /// <summary>
        /// Raised when a controller of a not yet known assembly is registered.
        /// </summary>
        public static event Action<Assembly> ControllerAssemblyRegistered;

        public static IEnumerable<Assembly> Assemblies => _assemblies.Keys.ToArray();

        public static void Register(Type controllerType, string areaName)
        {
            var changed = false;
            if (!_areas.TryGetValue(controllerType, out var existing) || existing != areaName)
            {
                _areas[controllerType] = areaName;
                changed = true;
            }

            if (_assemblies.TryAdd(controllerType.Assembly, true))
            {
                ControllerAssemblyRegistered?.Invoke(controllerType.Assembly);
                changed = true;
            }

            if (changed)
            {
                OrchardActionDescriptorChangeProvider.Instance.NotifyChanged();
            }
        }

        public static string GetArea(Type controllerType)
        {
            return _areas.TryGetValue(controllerType, out var area) ? area : null;
        }

        public void Apply(ControllerModel controller)
        {
            var area = GetArea(controller.ControllerType.AsType());
            if (area != null)
            {
                controller.RouteValues["area"] = area;
            }
        }
    }

    /// <summary>
    /// Signals ASP.NET Core MVC to rebuild its action descriptors when new controllers are registered.
    /// </summary>
    public class OrchardActionDescriptorChangeProvider : IActionDescriptorChangeProvider
    {
        public static OrchardActionDescriptorChangeProvider Instance { get; } = new OrchardActionDescriptorChangeProvider();

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public IChangeToken GetChangeToken()
        {
            return new CancellationChangeToken(_tokenSource.Token);
        }

        public void NotifyChanged()
        {
            var previous = Interlocked.Exchange(ref _tokenSource, new CancellationTokenSource());
            previous.Cancel();
        }
    }
}
