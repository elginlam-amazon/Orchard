using System;

namespace Orchard.Mvc.AntiForgery
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateAntiForgeryTokenOrchardAttribute : Attribute
    {
        public ValidateAntiForgeryTokenOrchardAttribute() : this(true) { }

        public ValidateAntiForgeryTokenOrchardAttribute(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; } = true;
    }
}
