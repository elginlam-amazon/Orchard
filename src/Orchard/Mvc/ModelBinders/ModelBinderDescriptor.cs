using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orchard.Mvc.ModelBinders
{
    public class ModelBinderDescriptor
    {
        public Type Type { get; set; }
        public IModelBinder ModelBinder { get; set; }
    }
}