using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Orchard.ContentManagement;

namespace Orchard.AuditTrail.Services.Models
{
    public class Filters : Dictionary<string, string>
    {

        public Filters(IUpdateModel updateModel)
        {
            UpdateModel = updateModel;
        }

        public IUpdateModel UpdateModel { get; set; }

        public static Filters From(IQueryCollection nameValues, IUpdateModel updateModel)
        {
            var filters = new Filters(updateModel);

            foreach (var nameValue in nameValues)
            {
                if (!string.IsNullOrEmpty(nameValue.Key))
                {
                    filters.Add(nameValue.Key, nameValue.Value.ToString());
                }
            }

            return filters;
        }

        public Filters AddFilter(string key, string value)
        {
            Add(key, value);
            return this;
        }
    }
}