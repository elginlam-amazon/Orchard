using Microsoft.AspNetCore.Mvc.Rendering;

namespace Orchard.Mvc.Html
{
    public class MvcFormAntiForgeryPost : MvcForm
    {
        private readonly IHtmlHelper _htmlHelper;

        public MvcFormAntiForgeryPost(IHtmlHelper htmlHelper) : base(htmlHelper.ViewContext, System.Text.Encodings.Web.HtmlEncoder.Default)
        {
            _htmlHelper = htmlHelper;
        }

        protected override void GenerateEndForm()
        {
            _htmlHelper.ViewContext.Writer.Write(_htmlHelper.AntiForgeryTokenOrchard());
            base.GenerateEndForm();
        }
    }
}
