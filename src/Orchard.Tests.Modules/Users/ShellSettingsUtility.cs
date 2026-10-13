using System.Security.Cryptography;
using Orchard.Environment.Configuration;
using Orchard.Utility.Extensions;

namespace Orchard.Tests.Modules.Users
{
    public class ShellSettingsUtility
    {
        public static ShellSettings CreateEncryptionEnabled()
        {

            const string encryptionAlgorithm = "AES";
            const string hashAlgorithm = "HMACSHA256";

            return new ShellSettings
            {
                Name = "Alpha",
                RequestUrlHost = "wiki.example.com",
                RequestUrlPrefix = "~/foo",
                EncryptionAlgorithm = encryptionAlgorithm,
                EncryptionKey = Orchard.Security.CryptographyAlgorithms.CreateSymmetricAlgorithm(encryptionAlgorithm).Key.ToHexString(),
                HashAlgorithm = hashAlgorithm,
                HashKey = Orchard.Security.CryptographyAlgorithms.CreateHmac(hashAlgorithm).Key.ToHexString()
            };

        }
    }
}
