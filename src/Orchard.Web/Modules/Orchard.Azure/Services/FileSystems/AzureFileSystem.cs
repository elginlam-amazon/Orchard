using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Orchard.FileSystems.Media;

namespace Orchard.Azure.Services.FileSystems
{

    /// <summary>
    /// A file system on top of an Azure Blob Storage container (Azure.Storage.Blobs, formerly WindowsAzure.Storage).
    /// Folders are virtual directories of blob names, materialized by a hidden folder entry blob.
    /// </summary>
    public class AzureFileSystem
    {

        public const string FolderEntry = "$$$ORCHARD$$$.$$$";

        private readonly bool _isPrivate;
        private readonly IMimeTypeProvider _mimeTypeProvider;

        protected string _root;
        protected string _absoluteRoot;
        protected string _publicHostName;

        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _container;

        public string StorageConnectionString
        {
            get;
            protected set;
        }

        public string ContainerName
        {
            get;
            protected set;
        }

        public BlobServiceClient BlobClient
        {
            get
            {
                EnsureInitialized();
                return _blobServiceClient;
            }
        }

        public BlobContainerClient Container
        {
            get
            {
                EnsureInitialized();
                return _container;
            }
        }

        public AzureFileSystem(string storageConnectionString, string containerName, string root, bool isPrivate, IMimeTypeProvider mimeTypeProvider, string publicHostName = null)
        {
            _isPrivate = isPrivate;
            _mimeTypeProvider = mimeTypeProvider;
            StorageConnectionString = storageConnectionString;
            ContainerName = containerName;
            _root = string.IsNullOrEmpty(root) ? "" : root + "/";
            _publicHostName = publicHostName;
        }

        protected void EnsureInitialized()
        {
            if (_blobServiceClient != null)
            {
                return;
            }

            var blobServiceClient = new BlobServiceClient(StorageConnectionString);
            _absoluteRoot = Combine(Combine(blobServiceClient.Uri.AbsoluteUri, ContainerName), _root);

            // Get and create the container if it does not exist
            // The container is named with DNS naming restrictions (i.e. all lower case)
            _container = blobServiceClient.GetBlobContainerClient(ContainerName);

            ////Set the default service version to the current version "2015-12-11" so that newer features and optimizations are enabled on the images and videos stored in the blob storage.
            var properties = blobServiceClient.GetProperties().Value;
            if (properties.DefaultServiceVersion == null)
            {
                properties.DefaultServiceVersion = "2015-12-11";
                blobServiceClient.SetProperties(properties);
            }

            _container.CreateIfNotExists(_isPrivate ? PublicAccessType.None : PublicAccessType.Blob);
            _blobServiceClient = blobServiceClient;
        }

        private static string ConvertToRelativeUriPath(string path)
        {
            var newPath = path.Replace(@"\", "/");

            if (newPath.StartsWith("/", StringComparison.OrdinalIgnoreCase) || newPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || newPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Path must be relative");
            }

            return newPath;
        }

        private static string GetFolderName(string path) => path.TrimEnd('/').Substring(path.TrimEnd('/').LastIndexOf('/') + 1);

        public string Combine(string path1, string path2)
        {
            if (path1 == null)
            {
                throw new ArgumentNullException("path1");
            }

            if (path2 == null)
            {
                throw new ArgumentNullException("path2");
            }

            if (string.IsNullOrEmpty(path2))
            {
                return path1;
            }

            if (string.IsNullOrEmpty(path1))
            {
                return path2;
            }

            if (path2.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path2.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return path2;
            }

            return (path1.TrimEnd('/') + '/' + path2.TrimStart('/'));
        }

        public IStorageFile GetFile(string path)
        {

            path = ConvertToRelativeUriPath(path);

            Container.EnsureBlobExists(string.Concat(_root, path));
            return new AzureBlobFileStorage(Container, Container.GetBlockBlobClient(string.Concat(_root, path)), null, _absoluteRoot);
        }

        public bool FileExists(string path)
        {
            return Container.BlobExists(string.Concat(_root, path));
        }

        public bool FolderExists(string path)
        {
            return Container.DirectoryExists(string.Concat(_root, path));
        }

        public IEnumerable<IStorageFile> ListFiles(string path)
        {

            path = path ?? string.Empty;
            path = ConvertToRelativeUriPath(path);

            var prefix = CloudBlobContainerExtensions.ToDirectoryPrefix(Combine(_root, path));

            return Container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", prefix, default)
                .Where(item => item.IsBlob && !item.Blob.Name.EndsWith(FolderEntry))
                .Select(item => new AzureBlobFileStorage(Container, Container.GetBlockBlobClient(item.Blob.Name), item.Blob.Properties, _absoluteRoot))
                .ToArray();
        }

        public IEnumerable<IStorageFolder> ListFolders(string path)
        {

            path = path ?? string.Empty;
            path = ConvertToRelativeUriPath(path);

            // return root folders
            if (string.Concat(_root, path) == string.Empty)
            {
                return Container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", null, default)
                    .Where(item => item.IsPrefix)
                    .Select<BlobHierarchyItem, IStorageFolder>(item => new AzureBlobFolderStorage(Container, item.Prefix, _absoluteRoot))
                    .ToList();
            }

            if (!Container.DirectoryExists(string.Concat(_root, path)))
            {
                try
                {
                    CreateFolder(path);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(string.Format("The folder could not be created at path: {0}. {1}",
                                                                path, ex));
                }
            }

            return Container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", CloudBlobContainerExtensions.ToDirectoryPrefix(string.Concat(_root, path)), default)
                .Where(item => item.IsPrefix)
                .Select<BlobHierarchyItem, IStorageFolder>(item => new AzureBlobFolderStorage(Container, item.Prefix, _absoluteRoot))
                .ToList();
        }

        public bool TryCreateFolder(string path)
        {
            try
            {
                if (!Container.DirectoryExists(string.Concat(_root, path)))
                {
                    CreateFolder(path);
                    return true;
                }

                // return false to be consistent with FileSystemProvider's implementation
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void CreateFolder(string path)
        {
            path = ConvertToRelativeUriPath(path);

            if (FileSystemStorageProvider.FolderNameContainsInvalidCharacters(GetFolderName(path)))
            {
                throw new InvalidNameCharacterException("The directory name contains invalid character(s)");
            }

            Container.EnsureDirectoryDoesNotExist(string.Concat(_root, path));

            // Creating a virtually hidden file to make the directory an existing concept
            CreateFile(Combine(path, FolderEntry));

            int lastIndex;
            while ((lastIndex = path.LastIndexOf('/')) > 0)
            {
                path = path.Substring(0, lastIndex);
                if (!Container.DirectoryExists(string.Concat(_root, path)))
                {
                    CreateFile(Combine(path, FolderEntry));
                }
            }
        }

        public void DeleteFolder(string path)
        {
            path = ConvertToRelativeUriPath(path);

            Container.EnsureDirectoryExists(string.Concat(_root, path));

            // a flat listing of the blobs under the directory includes the ones of the sub directories
            foreach (var blob in Container.GetBlobs(BlobTraits.None, BlobStates.None, CloudBlobContainerExtensions.ToDirectoryPrefix(string.Concat(_root, path)), default).ToArray())
            {
                Container.DeleteBlobIfExists(blob.Name);
            }
        }

        public void RenameFolder(string path, string newPath)
        {
            path = ConvertToRelativeUriPath(path);
            newPath = ConvertToRelativeUriPath(newPath);

            if (FileSystemStorageProvider.FolderNameContainsInvalidCharacters(GetFolderName(newPath)))
            {
                throw new InvalidNameCharacterException("The new directory name contains invalid character(s)");
            }

            // Workaround for https://github.com/Azure/azure-storage-net/issues/892.
            // Renaming a folder by only changing the casing corrupts all the files in the folder.
            if (path.Equals(newPath, StringComparison.OrdinalIgnoreCase))
            {
                var tempPath = Guid.NewGuid().ToString() + "/";

                RenameFolder(path, tempPath);

                path = tempPath;
            }

            if (!path.EndsWith("/"))
                path += "/";

            if (!newPath.EndsWith("/"))
                newPath += "/";

            var prefix = _root + path;
            foreach (var item in Container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", prefix, default).ToArray())
            {
                if (item.IsBlob)
                {
                    string filename = item.Blob.Name.Substring(prefix.Length);
                    string source = string.Concat(path, filename);
                    string destination = string.Concat(newPath, filename);
                    RenameFile(source, destination);
                }

                if (item.IsPrefix)
                {
                    string foldername = item.Prefix.Substring(prefix.Length);
                    string source = string.Concat(path, foldername);
                    string destination = string.Concat(newPath, foldername);
                    RenameFolder(source, destination);
                }
            }
        }

        public void DeleteFile(string path)
        {
            path = ConvertToRelativeUriPath(path);

            Container.EnsureBlobExists(Combine(_root, path));
            var blob = Container.GetBlobClient(Combine(_root, path));
            blob.DeleteIfExists();
        }

        public void RenameFile(string path, string newPath)
        {
            path = ConvertToRelativeUriPath(path);
            newPath = ConvertToRelativeUriPath(newPath);

            if (FileSystemStorageProvider.FileNameContainsInvalidCharacters(Path.GetFileName(newPath)))
            {
                throw new InvalidNameCharacterException("The new file name contains invalid character(s)");
            }

            Container.EnsureBlobExists(string.Concat(_root, path));
            Container.EnsureBlobDoesNotExist(string.Concat(_root, newPath));

            var blob = Container.GetBlobClient(string.Concat(_root, path));
            var newBlob = Container.GetBlobClient(string.Concat(_root, newPath));
            newBlob.StartCopyFromUri(blob.Uri).WaitForCompletion();
            blob.Delete();
        }

        public void CopyFile(string path, string newPath)
        {
            path = ConvertToRelativeUriPath(path);
            newPath = ConvertToRelativeUriPath(newPath);

            Container.EnsureBlobExists(string.Concat(_root, path));
            Container.EnsureBlobDoesNotExist(string.Concat(_root, newPath));

            var blob = Container.GetBlobClient(string.Concat(_root, path));
            var newBlob = Container.GetBlobClient(string.Concat(_root, newPath));
            newBlob.StartCopyFromUri(blob.Uri).WaitForCompletion();
        }

        public IStorageFile CreateFile(string path)
        {
            path = ConvertToRelativeUriPath(path);

            if (FileSystemStorageProvider.FileNameContainsInvalidCharacters(Path.GetFileName(path)))
            {
                throw new InvalidNameCharacterException("The file name contains invalid character(s)");
            }

            if (Container.BlobExists(string.Concat(_root, path)))
            {
                throw new ArgumentException("File " + path + " already exists");
            }

            // create all folder entries in the hierarchy
            int lastIndex;
            var localPath = path;
            while ((lastIndex = localPath.LastIndexOf('/')) > 0)
            {
                localPath = localPath.Substring(0, lastIndex);
                var folder = Container.GetBlobClient(string.Concat(_root, Combine(localPath, FolderEntry)));
                folder.Upload(new MemoryStream(new byte[0]), overwrite: true);
            }

            var blob = Container.GetBlockBlobClient(string.Concat(_root, path));
            var options = new BlobUploadOptions();
            var contentType = _mimeTypeProvider.GetMimeType(path);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                options.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            }

            Container.GetBlobClient(blob.Name).Upload(new MemoryStream(new byte[0]), options);
            return new AzureBlobFileStorage(Container, blob, null, _absoluteRoot);
        }

        public string GetPublicUrl(string path)
        {
            path = ConvertToRelativeUriPath(path);
            var uriBuilder = new UriBuilder(Container.GetBlobClient(string.Concat(_root, path)).Uri);
            if (!string.IsNullOrEmpty(_publicHostName)) uriBuilder.Host = _publicHostName;
            return uriBuilder.Uri.ToString();
        }

        private class AzureBlobFileStorage : IStorageFile
        {
            private readonly BlobContainerClient _container;
            private readonly BlockBlobClient _blob;
            private BlobItemProperties _listingProperties;
            private readonly string _rootPath;

            public AzureBlobFileStorage(BlobContainerClient container, BlockBlobClient blob, BlobItemProperties listingProperties, string rootPath)
            {
                _container = container;
                _blob = blob;
                _listingProperties = listingProperties;
                _rootPath = rootPath;
            }

            public string GetPath()
            {
                return _blob.Uri.ToString().Substring(_rootPath.Length).Trim('/');
            }

            public string GetName()
            {
                return Path.GetFileName(GetPath());
            }

            public long GetSize()
            {
                if (_listingProperties != null && _listingProperties.ContentLength.HasValue)
                {
                    return _listingProperties.ContentLength.Value;
                }

                return _blob.GetProperties().Value.ContentLength;
            }

            public DateTime GetLastUpdated()
            {
                return _blob.GetProperties().Value.LastModified.DateTime;
            }

            public string GetFileType()
            {
                return Path.GetExtension(GetPath());
            }

            public Stream OpenRead()
            {
                return _blob.OpenRead();
            }

            public Stream OpenWrite()
            {
                _listingProperties = null;
                return _blob.OpenWrite(overwrite: true);
            }

            public Stream CreateFile()
            {
                // as opposed to the File System implementation, if nothing is done on the stream
                // the file will be emptied, because Azure doesn't implement FileMode.Truncate
                _blob.DeleteIfExists();
                _container.GetBlobClient(_blob.Name).Upload(new MemoryStream(new byte[0]), overwrite: true);
                return OpenWrite();
            }
        }

        private class AzureBlobFolderStorage : IStorageFolder
        {
            private readonly BlobContainerClient _container;
            private readonly string _prefix;
            private readonly string _rootPath;

            public AzureBlobFolderStorage(BlobContainerClient container, string prefix, string rootPath)
            {
                _container = container;
                _prefix = CloudBlobContainerExtensions.ToDirectoryPrefix(prefix);
                _rootPath = rootPath;
            }

            public string GetName() => GetFolderName(GetPath());

            public string GetPath()
            {
                var uri = _container.Uri.ToString().TrimEnd('/') + "/" + _prefix;
                return uri.Substring(Math.Min(_rootPath.Length, uri.Length)).Trim('/');
            }

            public long GetSize()
            {
                // a flat listing includes the blobs of the sub directories
                return _container.GetBlobs(BlobTraits.None, BlobStates.None, _prefix, default).Sum(blob => blob.Properties.ContentLength ?? 0);
            }

            public DateTime GetLastUpdated()
            {
                return DateTime.MinValue;
            }

            public IStorageFolder GetParent()
            {
                var trimmed = _prefix.TrimEnd('/');
                var index = trimmed.LastIndexOf('/');
                if (index > 0)
                {
                    return new AzureBlobFolderStorage(_container, trimmed.Substring(0, index + 1), _rootPath);
                }
                throw new ArgumentException("Directory " + _prefix + " does not have a parent directory");
            }
        }
    }
}
