using System.Collections.Generic;
using Orchard.Mvc.ModelBinders;

namespace Orchard.ContentManagement.MetaData.Models
{
    public class SettingsDictionary : Dictionary<string, string>
    {
        public SettingsDictionary() { }
        public SettingsDictionary(IDictionary<string, string> dictionary) : base(dictionary) { }

        public T TryGetModel<T>(string key) where T : class
        {
            return (T)DictionaryModelBinder.Bind(this, typeof(T), key);
        }

        public T TryGetModel<T>() where T : class
        {
            return TryGetModel<T>(typeof(T).Name);
        }

        public T GetModel<T>() where T : class, new()
        {
            return GetModel<T>(typeof(T).Name);
        }

        public T GetModel<T>(string key) where T : class, new()
        {
            return TryGetModel<T>(key) ?? new T();
        }
    }
}
