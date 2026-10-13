using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Orchard.Utility.Extensions;

namespace Orchard.Tests.Utility.Extensions
{
    [TestFixture]
    public class HttpRequestExtensionsTests
    {
        private static HttpRequest NewRequest()
        {
            return new DefaultHttpContext().Request;
        }

        [Test]
        public void IsLocalUrlShouldReturnFalseWhenUrlIsNullOrEmpty()
        {
            var request = NewRequest();

            Assert.That(request.IsLocalUrl(null), Is.False);
            Assert.That(request.IsLocalUrl("   "), Is.False);
            Assert.That(request.IsLocalUrl(""), Is.False);
        }

        [Test]
        public void IsLocalUrlShouldReturnFalseWhenUrlStartsWithDoubleSlash()
        {
            var request = NewRequest();

            Assert.That(request.IsLocalUrl("//"), Is.False);
            Assert.That(request.IsLocalUrl("  //"), Is.False);
        }

        [Test]
        public void IsLocalUrlShouldReturnFalseWhenUrlStartsWithForwardBackwardSlash()
        {
            var request = NewRequest();

            Assert.That(request.IsLocalUrl("/\\"), Is.False);
            Assert.That(request.IsLocalUrl(" /\\"), Is.False);
        }

        [Test]
        public void IsLocalUrlShouldReturnTrueWhenUrlStartsWithSlashAndAnythingElse()
        {
            var request = NewRequest();

            Assert.That(request.IsLocalUrl("/"), Is.True);
            Assert.That(request.IsLocalUrl("\t/"), Is.True);
            Assert.That(request.IsLocalUrl("/контакты"), Is.True);
            Assert.That(request.IsLocalUrl("/  "), Is.True);
            Assert.That(request.IsLocalUrl("/abc-def"), Is.True);
        }

        [Test]
        public void IsLocalUrlShouldReturnTrueWhenAuthoritiesMatch()
        {
            var request = NewRequest();
            request.Headers["Host"] = "localhost";

            Assert.That(request.IsLocalUrl("http://localhost"), Is.True);
            Assert.That(request.IsLocalUrl("https://localhost"), Is.True);
        }

        [Test]
        public void IsLocalUrlShouldReturnFalseForNonHttpSchemes()
        {
            var request = NewRequest();
            request.Headers["Host"] = "localhost";

            Assert.That(request.IsLocalUrl("httpx://localhost"), Is.False);
            Assert.That(request.IsLocalUrl("foo://localhost"), Is.False);
            Assert.That(request.IsLocalUrl("data://localhost"), Is.False);
            Assert.That(request.IsLocalUrl("mailto://localhost"), Is.False);
        }

        [Test]
        public void IsLocalUrlShouldReturnFalseWhenAuthoritiesDiffer()
        {
            var request = NewRequest();
            request.Headers["Host"] = "localhost";

            Assert.That(request.IsLocalUrl("http://somedomain"), Is.False);
            Assert.That(request.IsLocalUrl("http://localhost:8080"), Is.False);
        }

        [Test]
        public void IsLocalUrlShouldReturnFalseForEverythingElse()
        {
            var request = NewRequest();
            request.Headers["Host"] = "localhost";

            Assert.That(request.IsLocalUrl("abc"), Is.False);
        }
    }
}
