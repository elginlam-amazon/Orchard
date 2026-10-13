using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.Mvc.ModelBinders
{
    /// <summary>
    /// Application wide registry of the model binders published by the tenants.
    /// It is consulted by <see cref="OrchardModelBinderProvider"/> which is registered in the MVC options.
    /// </summary>
    public class ModelBinderDictionary
    {
        private readonly ConcurrentDictionary<Type, IModelBinder> _binders = new ConcurrentDictionary<Type, IModelBinder>();

        public IModelBinder this[Type type]
        {
            get { return _binders.TryGetValue(type, out var binder) ? binder : null; }
            set { _binders[type] = value; }
        }

        public bool TryGetValue(Type type, out IModelBinder binder)
        {
            return _binders.TryGetValue(type, out binder);
        }
    }

    /// <summary>
    /// ASP.NET Core model binder provider returning the binders published in the <see cref="ModelBinderDictionary"/>.
    /// </summary>
    public class OrchardModelBinderProvider : Microsoft.AspNetCore.Mvc.ModelBinding.IModelBinderProvider
    {
        private readonly ModelBinderDictionary _binders;

        public OrchardModelBinderProvider(ModelBinderDictionary binders)
        {
            _binders = binders;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            return _binders.TryGetValue(context.Metadata.ModelType, out var binder) ? binder : null;
        }
    }
}
