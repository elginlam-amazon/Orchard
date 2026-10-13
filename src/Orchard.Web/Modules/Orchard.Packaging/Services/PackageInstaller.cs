using System;
using System.IO;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.VirtualPath;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Packaging.Extensions;
using Orchard.Packaging.Models;
using Orchard.Packaging.Packages;
using Orchard.Services;
using Orchard.UI;
using Orchard.UI.Notify;

namespace Orchard.Packaging.Services
{
    [OrchardFeature("PackagingServices")]
    public class PackageInstaller : IPackageInstaller
    {
        private readonly INotifier _notifier;
        private readonly IVirtualPathProvider _virtualPathProvider;
        private readonly IExtensionManager _extensionManager;
        private readonly IFolderUpdater _folderUpdater;
        private readonly IClock _clock;

        public PackageInstaller(
            INotifier notifier,
            IVirtualPathProvider virtualPathProvider,
            IExtensionManager extensionManager,
            IFolderUpdater folderUpdater,
            IClock clock)
        {

            _notifier = notifier;
            _virtualPathProvider = virtualPathProvider;
            _extensionManager = extensionManager;
            _folderUpdater = folderUpdater;
            _clock = clock;

            T = NullLocalizer.Instance;
            Logger = Logging.NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public Logging.ILogger Logger { get; set; }

        public PackageInfo Install(string packageId, string version, string location, string applicationPath)
        {
            // instantiates the appropriate package repository
            var packageRepository = new PackageRepository(location);

            // gets an IPackage instance from the repository
            var packageVersion = string.IsNullOrEmpty(version) ? null : new Version(version);
            var package = packageRepository.FindPackage(packageId, packageVersion);
            if (package == null)
            {
                var message = T("The specified package could not be found: ID: {0}, version: {1}.",
                    packageId, string.IsNullOrEmpty(version) ? T("No version").Text : version);
                throw new OrchardException(message);
            }

            return InstallPackage(package, location, applicationPath);
        }

        public PackageInfo Install(IPackage package, string location, string applicationPath)
        {
            return InstallPackage(package, location, applicationPath);
        }

        protected PackageInfo InstallPackage(IPackage package, string location, string applicationPath)
        {
            bool previousInstalled;

            // 1. See if extension was previous installed and backup its folder if so
            try
            {
                previousInstalled = BackupExtensionFolder(package.ExtensionFolder(), package.ExtensionId());
            }
            catch (Exception exception)
            {
                throw new OrchardException(T("Unable to backup existing local package directory."), exception);
            }

            if (previousInstalled)
            {
                // 2. If extension is installed, need to un-install first
                try
                {
                    UninstallExtensionIfNeeded(package);
                }
                catch (Exception exception)
                {
                    throw new OrchardException(T("Unable to un-install local package before updating."), exception);
                }
            }

            var packageInfo = ExecuteInstall(package, applicationPath);

            // check the new package is compatible with current Orchard version
            var descriptor = package.GetExtensionDescriptor(packageInfo.ExtensionType);

            if (descriptor != null)
            {
                if (new FlatPositionComparer().Compare(descriptor.OrchardVersion, typeof(ContentItem).Assembly.GetName().Version.ToString()) >= 0)
                {
                    if (previousInstalled)
                    {
                        // restore the previous version
                        RestoreExtensionFolder(package.ExtensionFolder(), package.ExtensionId());
                    }
                    else
                    {
                        // just uninstall the new package
                        Uninstall(package.Id, _virtualPathProvider.MapPath("~\\"));
                    }

                    Logger.Error(string.Format("The package is compatible with version {0} and above. Please update Orchard or install another version of this package.", descriptor.OrchardVersion));
                    throw new OrchardException(T("The package is compatible with version {0} and above. Please update Orchard or install another version of this package.", descriptor.OrchardVersion));
                }
            }

            return packageInfo;
        }

        /// <summary>
        /// Executes a package installation: the "Content" files of the package are copied to the application.
        /// </summary>
        /// <param name="package">The package to install.</param>
        /// <param name="targetPath">The path where to install the package.</param>
        /// <returns>The package information.</returns>
        /// <remarks>
        /// The former NuGet.Core project manager also installed the package in the solution "packages" folder,
        /// which is not done anymore (it's not used by Orchard at runtime).
        /// </remarks>
        protected PackageInfo ExecuteInstall(IPackage package, string targetPath)
        {
            const string contentFolder = "Content";
            var now = _clock.UtcNow;

            foreach (var file in package.GetFiles())
            {
                var path = file.Path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                if (!path.StartsWith(contentFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = path.Substring(contentFolder.Length + 1);
                var fullPath = Path.GetFullPath(Path.Combine(targetPath, relativePath));

                // prevent the package from writing outside of the application
                if (!fullPath.StartsWith(Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                {
                    throw new OrchardException(T("The package file {0} is outside of the application folder.", file.Path));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                using (var source = file.GetStream())
                using (var destination = File.Create(fullPath))
                {
                    source.CopyTo(destination);
                }

                File.SetLastWriteTimeUtc(fullPath, now);
                Logger.Debug("Added file '{0}'", relativePath);
            }

            return new PackageInfo
            {
                ExtensionName = package.Title ?? package.Id,
                ExtensionVersion = package.Version.ToString(),
                ExtensionType = package.Id.StartsWith(PackagingSourceManager.GetExtensionPrefix(DefaultExtensionTypes.Theme)) ? DefaultExtensionTypes.Theme : DefaultExtensionTypes.Module,
                ExtensionPath = targetPath
            };
        }

        /// <summary>
        /// Uninstalls a package.
        /// </summary>
        /// <param name="packageId">The package identifier for the package to be uninstalled.</param>
        /// <param name="applicationPath">The application path.</param>
        public void Uninstall(string packageId, string applicationPath)
        {
            string extensionFullPath = string.Empty;

            if (packageId.StartsWith(PackagingSourceManager.GetExtensionPrefix(DefaultExtensionTypes.Theme)))
            {
                extensionFullPath = _virtualPathProvider.MapPath("~/Themes/" + packageId.Substring(PackagingSourceManager.GetExtensionPrefix(DefaultExtensionTypes.Theme).Length));
            }
            else if (packageId.StartsWith(PackagingSourceManager.GetExtensionPrefix(DefaultExtensionTypes.Module)))
            {
                extensionFullPath = _virtualPathProvider.MapPath("~/Modules/" + packageId.Substring(PackagingSourceManager.GetExtensionPrefix(DefaultExtensionTypes.Module).Length));
            }

            if (string.IsNullOrEmpty(extensionFullPath) ||
                !Directory.Exists(extensionFullPath))
            {

                throw new OrchardException(T("Package not found: {0}", packageId));
            }

            // The package is uninstalled by removing its directory
            if (Directory.Exists(extensionFullPath))
            {
                Directory.Delete(extensionFullPath, true);
            }
        }

        private bool RestoreExtensionFolder(string extensionFolder, string extensionId)
        {
            var source = new DirectoryInfo(_virtualPathProvider.MapPath(_virtualPathProvider.Combine("~", extensionFolder, extensionId)));

            if (source.Exists)
            {
                var tempPath = _virtualPathProvider.Combine("~", extensionFolder, "_Backup", extensionId);
                string localTempPath = null;
                for (int i = 0; i < 1000; i++)
                {
                    localTempPath = _virtualPathProvider.MapPath(tempPath) + (i == 0 ? "" : "." + i.ToString());
                    if (!Directory.Exists(localTempPath))
                    {
                        Directory.CreateDirectory(localTempPath);
                        break;
                    }
                    localTempPath = null;
                }

                if (localTempPath == null)
                {
                    throw new OrchardException(T("Backup folder {0} has too many backups subfolder (limit is 1,000)", tempPath));
                }

                var backupFolder = new DirectoryInfo(localTempPath);
                _folderUpdater.Restore(backupFolder, source);
                _notifier.Success(T("Successfully restored local package to local folder \"{0}\"", source));

                return true;
            }

            return false;
        }

        private bool BackupExtensionFolder(string extensionFolder, string extensionId)
        {
            var source = new DirectoryInfo(_virtualPathProvider.MapPath(_virtualPathProvider.Combine("~", extensionFolder, extensionId)));

            if (source.Exists)
            {
                var tempPath = _virtualPathProvider.Combine("~", extensionFolder, "_Backup", extensionId);
                string localTempPath = null;
                for (int i = 0; i < 1000; i++)
                {
                    localTempPath = _virtualPathProvider.MapPath(tempPath) + (i == 0 ? "" : "." + i.ToString());
                    if (!Directory.Exists(localTempPath))
                    {
                        Directory.CreateDirectory(localTempPath);
                        break;
                    }
                    localTempPath = null;
                }

                if (localTempPath == null)
                {
                    throw new OrchardException(T("Backup folder {0} has too many backups subfolder (limit is 1,000)", tempPath));
                }

                var backupFolder = new DirectoryInfo(localTempPath);
                _folderUpdater.Backup(source, backupFolder);
                _notifier.Success(T("Successfully backed up local package to local folder \"{0}\"", backupFolder));

                return true;
            }

            return false;
        }

        private void UninstallExtensionIfNeeded(IPackage package)
        {
            // Nuget requires to un-install the currently installed packages if the new
            // package is the same version or an older version
            try
            {
                Uninstall(package.Id, _virtualPathProvider.MapPath("~\\"));
                _notifier.Success(T("Successfully un-installed local package {0}", package.ExtensionId()));
            }
            catch { }
        }
    }
}