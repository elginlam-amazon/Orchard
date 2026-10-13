using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orchard.Core.XmlRpc.Services;
using Orchard.Mvc.ModelBinders;
using IModelBinderProvider = Orchard.Mvc.ModelBinders.IModelBinderProvider;

namespace Orchard.Core.XmlRpc.Models
{
    public class ModelBinderProvider : IModelBinderProvider, IModelBinder
    {
        private readonly IXmlRpcReader _mapper;

        public ModelBinderProvider(IXmlRpcReader mapper)
        {
            _mapper = mapper;
        }

        public IEnumerable<ModelBinderDescriptor> GetModelBinders()
        {
            return new[] {
                             new ModelBinderDescriptor {
                                                           ModelBinder = this,
                                                           Type = typeof(XRpcMethodCall)
                                                       }
                         };
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            // Ah! xmlrpc is a value provider!!!
            // TODO: refactor this? 
            using (var xmlReader = XmlReader.Create(bindingContext.HttpContext.Request.Body, new XmlReaderSettings { Async = true }))
            {
                var element = await XElement.LoadAsync(xmlReader, LoadOptions.None, bindingContext.HttpContext.RequestAborted);
                bindingContext.Result = ModelBindingResult.Success(_mapper.MapToMethodCall(element));
            }
        }
    }
}