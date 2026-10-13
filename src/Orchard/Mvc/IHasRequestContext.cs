using Microsoft.AspNetCore.Mvc;

namespace Orchard.Mvc
{
    public interface IHasRequestContext
    {
        ActionContext ActionContext { get; }
    }
}