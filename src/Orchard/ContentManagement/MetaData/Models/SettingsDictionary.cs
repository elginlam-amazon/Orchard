using System.Collections.Generic;

namespace Orchard.ContentManagement.MetaData.Models
{
    public class SettingsDictionary : Dictionary<string, string>
    {
        public SettingsDictionary() { }
        public SettingsDictionary(IDictionary<string, string> dictionary) : base(dictionary) { }

        public T TryGetModel<T>(string key) where T : class
        {
            // Model binding from dictionary not supported in this migration stage
            return null;
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
