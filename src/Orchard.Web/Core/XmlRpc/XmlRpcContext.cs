using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orchard.Core.XmlRpc.Models;

namespace Orchard.Core.XmlRpc
{
    public class XmlRpcContext
    {
        public ControllerContext ControllerContext { get; set; }
        public HttpContext HttpContext { get; set; }
        public XRpcMethodCall Request { get; set; }
        public XRpcMethodResponse Response { get; set; }
        public ICollection<IXmlRpcDriver> _drivers = new List<IXmlRpcDriver>();
    }
}