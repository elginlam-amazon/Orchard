using System;
using Azure.Storage.Blobs;
using NUnit.Framework;
using Orchard.Azure.Services.Environment.Configuration;

namespace Orchard.Azure.Tests
{
    public abstract class AzureVirtualEnvironmentTest
    {
        protected IPlatformConfigurationAccessor PlatformConfigurationAccessor = new DefaultPlatformConfigurationAccessor();
        protected abstract string StorageConnectionStringName { get; }
        protected BlobServiceClient DevAccount { get; private set; }

        protected abstract void OnInit();

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var connectionString = PlatformConfigurationAccessor.GetSetting(StorageConnectionStringName, "default", "");

            // formerly CloudStorageAccount.TryParse: an invalid connection string leaves the account undefined
            try
            {
                DevAccount = string.IsNullOrEmpty(connectionString) ? null : new BlobServiceClient(connectionString);
            }
            catch (FormatException)
            {
                DevAccount = null;
            }

            OnInit();
        }

        protected void DeleteAllBlobs(string containerName, BlobServiceClient account)
        {
            var container = account.GetBlobContainerClient(containerName);

            // the flat listing returns the blobs of all the virtual directories
            foreach (var blob in container.GetBlobs())
            {
                container.DeleteBlobIfExists(blob.Name);
            }
        }
    }
}
