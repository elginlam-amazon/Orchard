using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;

namespace Orchard.Mvc.Wrappers
{
    public abstract class HttpResponseBaseWrapper : HttpResponseBase
    {
        private readonly HttpResponseBase _httpResponseBase;

        protected HttpResponseBaseWrapper(HttpResponseBase httpResponse)
        {
            _httpResponseBase = httpResponse;
        }

        public override void AddHeader(string name, string value) => _httpResponseBase.AddHeader(name, value);
        public override void AppendCookie(HttpCookie cookie) => _httpResponseBase.AppendCookie(cookie);
        public override void AppendHeader(string name, string value) => _httpResponseBase.AppendHeader(name, value);
        public new string ApplyAppPathModifier(string virtualPath) => virtualPath;
        public override void BinaryWrite(byte[] buffer) => _httpResponseBase.BinaryWrite(buffer);
        public override void Clear() => _httpResponseBase.Clear();
        public override void ClearContent() => _httpResponseBase.ClearContent();
        public override void ClearHeaders() => _httpResponseBase.ClearHeaders();
        public override void End() => _httpResponseBase.End();
        public override void Flush() => _httpResponseBase.Flush();
        public override void Redirect(string url) => _httpResponseBase.Redirect(url);
        public override void Redirect(string url, bool endResponse) => _httpResponseBase.Redirect(url, endResponse);
        public override void SetCookie(HttpCookie cookie) => _httpResponseBase.SetCookie(cookie);
        public override void TransmitFile(string filename) => _httpResponseBase.TransmitFile(filename);
        public override void Write(char ch) => _httpResponseBase.Write(ch);
        public override void Write(object obj) => _httpResponseBase.Write(obj);
        public override void Write(string s) => _httpResponseBase.Write(s);
        public override void WriteFile(string filename) => _httpResponseBase.WriteFile(filename);
        public new void WriteSubstitution(HttpResponseSubstitutionCallback callback) { }

        public override bool BufferOutput
        {
            get => _httpResponseBase.BufferOutput;
            set => _httpResponseBase.BufferOutput = value;
        }

        public override HttpCachePolicyBase Cache => _httpResponseBase.Cache;

        public override string Charset
        {
            get => _httpResponseBase.Charset;
            set => _httpResponseBase.Charset = value;
        }

        public override Encoding ContentEncoding
        {
            get => _httpResponseBase.ContentEncoding;
            set => _httpResponseBase.ContentEncoding = value;
        }

        public override string ContentType
        {
            get => _httpResponseBase.ContentType;
            set => _httpResponseBase.ContentType = value;
        }

        public override HttpCookieCollection Cookies => _httpResponseBase.Cookies;

        public override NameValueCollection Headers => _httpResponseBase.Headers;

        public override bool IsClientConnected => _httpResponseBase.IsClientConnected;

        public override bool IsRequestBeingRedirected => _httpResponseBase.IsRequestBeingRedirected;

        public override TextWriter Output => _httpResponseBase.Output;

        public override Stream OutputStream => _httpResponseBase.OutputStream;

        public override string RedirectLocation
        {
            get => _httpResponseBase.RedirectLocation;
            set => _httpResponseBase.RedirectLocation = value;
        }

        public override int StatusCode
        {
            get => _httpResponseBase.StatusCode;
            set => _httpResponseBase.StatusCode = value;
        }

        public override string StatusDescription
        {
            get => _httpResponseBase.StatusDescription;
            set => _httpResponseBase.StatusDescription = value;
        }

        public override bool SuppressContent
        {
            get => _httpResponseBase.SuppressContent;
            set => _httpResponseBase.SuppressContent = value;
        }

        public override bool TrySkipIisCustomErrors
        {
            get => _httpResponseBase.TrySkipIisCustomErrors;
            set => _httpResponseBase.TrySkipIisCustomErrors = value;
        }
    }
}
