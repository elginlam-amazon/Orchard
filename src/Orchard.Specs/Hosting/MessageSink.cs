using System.Collections.Generic;
using System.Diagnostics;

namespace Orchard.Specs.Hosting
{
    public class MessageSink
    {
        readonly IList<string> _messages = new List<string>();

        public void Receive(string message)
        {
            Trace.WriteLine("  " + message);
            _messages.Add(message);
        }
    }
}
