using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Orchard.Mvc.Spooling
{
    public class HtmlStringWriter : TextWriter, IHtmlContent
    {
        private readonly TextWriter _writer;

        public HtmlStringWriter()
        {
            _writer = new StringWriter();
        }

        public override Encoding Encoding => _writer.Encoding;

        public string ToHtmlString()
        {
            return _writer.ToString();
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write(_writer.ToString());
        }

        public override string ToString()
        {
            return _writer.ToString();
        }

        public override void Write(string value)
        {
            _writer.Write(value);
        }

        public override void Write(char value)
        {
            _writer.Write(value);
        }
    }
}
