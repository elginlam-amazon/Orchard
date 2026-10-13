using System.IO;
using NUnit.Framework;
using Orchard.Web.Hosting;

namespace Orchard.Web.Tests.Hosting
{
    /// <summary>
    /// The static files are only served from the folders which allow it with their Web.config file
    /// (the former IIS handlers configuration, the application Web.config cleared all the handlers).
    /// </summary>
    [TestFixture]
    public class StaticFileAccessPolicyTests
    {
        private string _contentRoot;

        [SetUp]
        public void Init()
        {
            _contentRoot = Path.Combine(Path.GetTempPath(), "Orchard.Web.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(_contentRoot, "Modules", "Foo", "Scripts", "lib"));
            File.WriteAllText(Path.Combine(_contentRoot, "Modules", "Foo", "Module.txt"), "Name: Foo");
            File.WriteAllText(Path.Combine(_contentRoot, "Modules", "Foo", "Scripts", "foo.js"), "");
            File.WriteAllText(Path.Combine(_contentRoot, "Modules", "Foo", "Scripts", "lib", "bar.js"), "");
            File.WriteAllText(Path.Combine(_contentRoot, "Modules", "Foo", "Scripts", "Web.config"),
                "<configuration><system.webServer><handlers accessPolicy=\"Script,Read\">" +
                "<add name=\"StaticFile\" path=\"*\" verb=\"*\" modules=\"StaticFileModule\" preCondition=\"integratedMode\" resourceType=\"File\" requireAccess=\"Read\" />" +
                "</handlers></system.webServer></configuration>");
            File.WriteAllText(Path.Combine(_contentRoot, "Modules", "Foo", "Web.config"),
                "<configuration><system.webServer><handlers accessPolicy=\"Script\"><clear /></handlers></system.webServer></configuration>");
        }

        [TearDown]
        public void Cleanup()
        {
            Directory.Delete(_contentRoot, true);
        }

        [Test]
        public void FilesOfStaticFileFoldersAreAllowed()
        {
            Assert.That(StaticFileAccessPolicy.IsAllowed(_contentRoot, Path.Combine(_contentRoot, "Modules", "Foo", "Scripts", "foo.js")), Is.True);
        }

        [Test]
        public void FilesOfSubFoldersOfStaticFileFoldersAreAllowed()
        {
            Assert.That(StaticFileAccessPolicy.IsAllowed(_contentRoot, Path.Combine(_contentRoot, "Modules", "Foo", "Scripts", "lib", "bar.js")), Is.True);
        }

        [Test]
        public void ExtensionFilesAreNotAllowed()
        {
            Assert.That(StaticFileAccessPolicy.IsAllowed(_contentRoot, Path.Combine(_contentRoot, "Modules", "Foo", "Module.txt")), Is.False);
        }

        [Test]
        public void FilesOutsideTheApplicationAreNotAllowed()
        {
            Assert.That(StaticFileAccessPolicy.IsAllowed(_contentRoot, Path.Combine(Path.GetTempPath(), "foo.js")), Is.False);
            Assert.That(StaticFileAccessPolicy.IsAllowed(_contentRoot, null), Is.False);
        }
    }
}
