using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Orchard.Specs.Hosting
{
    /// <summary>
    /// The "Simple.Web" site template, used to verify the hosting of the specs (formerly an ASP.NET application
    /// with Web Forms pages and a routed handler).
    /// </summary>
    public static class SimpleWebApplication
    {
        public static void Configure(WebApplication app)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(app.Environment.ContentRootPath),
                ServeUnknownFileTypes = false
            });

            // The routed request (formerly the HelloYetAgainHandler route handler).
            app.MapGet("/hello-world", () => Results.Text("Hello yet again", "text/html"));

            // The former Web Forms pages of the Simple folder.
            app.MapGet("/Simple/Page.aspx", (HttpContext context) => Html(
                "<p>Hello again</p>" +
                "<p>RawUrl: " + WebUtility.HtmlEncode(context.Request.PathBase + context.Request.Path + context.Request.QueryString) + "</p>" +
                "<p>Moving along to <a href=\"/hello-world\">next page</a></p>" +
                "<p><form action=\"" + context.Request.PathBase + "/Simple/Results.aspx\" method=\"post\">" +
                "<input type=\"text\" name=\"passthrough1\" value=\"alpha\" />" +
                "<input type=\"hidden\" name=\"passthrough2\" value=\"beta\" />" +
                "<input type=\"hidden\" name=\"input1\" />" +
                "<input type=\"submit\" value=\"Go!\" />" +
                "</form></p>"));

            app.MapPost("/Simple/Results.aspx", async (HttpContext context) =>
            {
                var form = await context.Request.ReadFormAsync();
                return Html(
                    "<ul>" +
                    "<li>passthrough1:" + WebUtility.HtmlEncode(form["passthrough1"]) + "</li>" +
                    "<li>passthrough2:" + WebUtility.HtmlEncode(form["passthrough2"]) + "</li>" +
                    "<li>input1:" + WebUtility.HtmlEncode(form["input1"]) + "</li>" +
                    "</ul>");
            });

            app.MapGet("/Simple/Cookie-Set.aspx", (HttpContext context) =>
            {
                context.Response.Cookies.Append("foo", "bar");
                return Html(string.Empty);
            });

            app.MapGet("/Simple/Cookie-Show.aspx", (HttpContext context) => Html(
                "<ul>" +
                string.Concat(context.Request.Cookies.Select(cookie => "<li>" + cookie.Key + ":" + cookie.Value + "</li>")) +
                "</ul>"));

            app.MapGet("/Simple/Redir.aspx", (HttpContext context) => Results.Redirect(context.Request.PathBase + "/Simple/Page.aspx"));
        }

        private static IResult Html(string body)
        {
            return Results.Content(body, "text/html; charset=utf-8");
        }
    }
}
