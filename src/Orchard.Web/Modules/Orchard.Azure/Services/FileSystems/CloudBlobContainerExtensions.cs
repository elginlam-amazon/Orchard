using System;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Orchard.Azure.Services.FileSystems
{

    public static class CloudBlobContainerExtensions
    {

        public static bool BlobExists(this BlobContainerClient container, string path)
        {
            if (string.IsNullOrEmpty(path) || path.Trim() == string.Empty)
                throw new ArgumentException("Path can't be empty");
            return container.GetBlobClient(path).Exists().Value;
        }

        public static void EnsureBlobExists(this BlobContainerClient container, string path)
        {
            if (!BlobExists(container, path))
            {
                throw new ArgumentException("File " + path + " does not exist");
            }
        }

        public static void EnsureBlobDoesNotExist(this BlobContainerClient container, string path)
        {
            if (BlobExists(container, path))
            {
                throw new ArgumentException("File " + path + " already exists");
            }
        }

        public static bool DirectoryExists(this BlobContainerClient container, string path)
        {
            if (string.IsNullOrEmpty(path) || path.Trim() == string.Empty)
            {
                throw new ArgumentException("Path can't be empty");
            }

            return container.GetBlobs(BlobTraits.None, BlobStates.None, ToDirectoryPrefix(path), default).Any();
        }

        public static void EnsureDirectoryExists(this BlobContainerClient container, string path)
        {
            if (!DirectoryExists(container, path))
            {
                throw new ArgumentException("Directory " + path + " does not exist");
            }
        }

        public static void EnsureDirectoryDoesNotExist(this BlobContainerClient container, string path)
        {
            if (DirectoryExists(container, path))
            {
                throw new ArgumentException("Directory " + path + " already exists");
            }
        }

        /// <summary>
        /// Returns the blob name prefix of a virtual directory (ending with the delimiter).
        /// </summary>
        public static string ToDirectoryPrefix(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.EndsWith("/") ? path : path + "/";
        }
    }
}
