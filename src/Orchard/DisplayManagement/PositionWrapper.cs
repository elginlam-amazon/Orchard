using System.IO;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Html;

namespace Orchard.DisplayManagement
{
    public class PositionWrapper : IHtmlContent, IPositioned
    {

        private IHtmlContent _value;
        public string Position { get; private set; }

        public PositionWrapper(IHtmlContent value, string position)
        {
            _value = value;
            Position = position;
        }

        public PositionWrapper(string value, string position)
            : this(new HtmlString(HttpUtility.HtmlEncode(value)), position)
        {
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            _value.WriteTo(writer, encoder);
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
