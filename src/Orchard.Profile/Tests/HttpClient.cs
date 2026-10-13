using System.Net;
using System.Net.Http;
using System.Text;
using Reqnroll;

namespace Orchard.Profile.Tests
{
    [Binding]
    public class HttpClient
    {
        private readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() });
        private string _text;

        [Given(@"I am logged in")]
        public void GivenIAmLoggedIn()
        {
            DoRequest("/Users/Account/LogOn");

            const string requestVerificationTokenName = "__RequestVerificationToken";
            const string valueMarker = "value=\"";

            var tokenIndex = _text.IndexOf(requestVerificationTokenName);
            var valueIndex = _text.IndexOf(valueMarker, tokenIndex);
            var valueStart = valueIndex + valueMarker.Length;
            var valueEnd = _text.IndexOf("\"", valueStart);
            var requestVerificationTokenValue = _text.Substring(valueStart, valueEnd - valueStart);

            DoRequest("/Users/Account/LogOn", "userNameOrEmail=admin&password=profiling-secret&rememberMe=false&" + requestVerificationTokenName + "=" + requestVerificationTokenValue);
        }

        [When(@"I go to ""(.*)""")]
        public void WhenIGoTo(string url)
        {
            DoRequest(url);
        }

        [When(@"I go to ""(.*)"" (.*) times")]
        public void WhenIGoTo(string url, int times)
        {
            for (int i = 0; i != times; ++i)
                DoRequest(url);
        }

        private void DoRequest(string url)
        {
            DoRequest(url, null);
        }

        private void DoRequest(string url, string postData)
        {
            HttpResponseMessage response;
            if (postData != null)
            {
                var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");
                response = _client.PostAsync("http://localhost" + url, content).GetAwaiter().GetResult();
            }
            else
            {
                response = _client.GetAsync("http://localhost" + url).GetAwaiter().GetResult();
            }

            // the error responses are read too
            using (response)
            {
                _text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
    }
}
