using System;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Orchard.Localization
{

    /// <summary>
    /// An HTML-encoded localized string
    /// </summary>
    public class LocalizedString : IHtmlContent
    {
        public LocalizedString(string languageNeutral)
        {
            Text = languageNeutral;
            TextHint = languageNeutral;
        }

        public LocalizedString(string localized, string scope, string textHint, object[] args)
        {
            Text = localized;
            Scope = scope;
            TextHint = textHint;
            Args = args;
        }

        public static LocalizedString TextOrDefault(string text, LocalizedString defaultValue)
        {
            if (string.IsNullOrEmpty(text))
                return defaultValue;
            return new LocalizedString(text);
        }

        public string Scope { get; }

        /// <summary>
        /// The HTML-Encoded original text
        /// </summary>
        public string TextHint { get; }

        public object[] Args { get; }

        /// <summary>
        /// The HTML-encoded localized text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The HTML-encoded localized text
        /// </summary>
        public override string ToString()
        {
            return Text;
        }

        /// <summary>
        /// Writes the HTML-encoded localized text, as is.
        /// </summary>
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write(Text);
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            if (Text != null)
                hashCode ^= Text.GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var that = (LocalizedString)obj;
            return string.Equals(Text, that.Text);
        }
    }
}
