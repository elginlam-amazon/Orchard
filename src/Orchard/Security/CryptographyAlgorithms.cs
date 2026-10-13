using System;
using System.Security.Cryptography;

namespace Orchard.Security
{
    /// <summary>
    /// Creates the cryptographic algorithms from their names (replaces the obsolete
    /// HashAlgorithm.Create(string), SymmetricAlgorithm.Create(string) and HMAC.Create(string) factories, SYSLIB0045).
    /// </summary>
    public static class CryptographyAlgorithms
    {
        public static HashAlgorithm CreateHashAlgorithm(string name)
        {
            switch ((name ?? string.Empty).ToUpperInvariant().Replace("-", string.Empty))
            {
                case "SHA1":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.SHA1":
                    return SHA1.Create();
                case "SHA256":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.SHA256":
                    return SHA256.Create();
                case "SHA384":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.SHA384":
                    return SHA384.Create();
                case "SHA512":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.SHA512":
                    return SHA512.Create();
                case "MD5":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.MD5":
                    return MD5.Create();
                default:
                    return CryptoConfig.CreateFromName(name) as HashAlgorithm
                        ?? throw new ArgumentException(string.Format("Unknown hash algorithm '{0}'.", name), nameof(name));
            }
        }

        public static SymmetricAlgorithm CreateSymmetricAlgorithm(string name)
        {
            switch ((name ?? string.Empty).ToUpperInvariant())
            {
                case "AES":
                case "RIJNDAEL":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.AES":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.RIJNDAELMANAGED":
                    return Aes.Create();
                case "TRIPLEDES":
                case "3DES":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.TRIPLEDES":
                    return TripleDES.Create();
                default:
                    return CryptoConfig.CreateFromName(name) as SymmetricAlgorithm
                        ?? throw new ArgumentException(string.Format("Unknown symmetric algorithm '{0}'.", name), nameof(name));
            }
        }

        public static HMAC CreateHmac(string name)
        {
            switch ((name ?? string.Empty).ToUpperInvariant())
            {
                case "HMACSHA1":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.HMACSHA1":
                    return new HMACSHA1();
                case "HMACSHA256":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.HMACSHA256":
                    return new HMACSHA256();
                case "HMACSHA384":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.HMACSHA384":
                    return new HMACSHA384();
                case "HMACSHA512":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.HMACSHA512":
                    return new HMACSHA512();
                case "HMACMD5":
                case "SYSTEM.SECURITY.CRYPTOGRAPHY.HMACMD5":
                    return new HMACMD5();
                default:
                    return CryptoConfig.CreateFromName(name) as HMAC
                        ?? throw new ArgumentException(string.Format("Unknown HMAC algorithm '{0}'.", name), nameof(name));
            }
        }
    }

    /// <summary>
    /// PBKDF2 password hashing, compatible with the hashes produced by the former System.Web.Helpers.Crypto
    /// (HMAC-SHA1, 1000 iterations, 128 bits salt, 256 bits subkey, format: 0x00 | salt | subkey, base64 encoded).
    /// </summary>
    public static class Crypto
    {
        private const int Pbkdf2IterCount = 1000;
        private const int Pbkdf2SubkeyLength = 256 / 8;
        private const int SaltSize = 128 / 8;

        public static string HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2IterCount, HashAlgorithmName.SHA1, Pbkdf2SubkeyLength);

            var outputBytes = new byte[1 + SaltSize + Pbkdf2SubkeyLength];
            Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, Pbkdf2SubkeyLength);
            return Convert.ToBase64String(outputBytes);
        }

        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            if (hashedPassword == null)
            {
                throw new ArgumentNullException(nameof(hashedPassword));
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            byte[] hashedPasswordBytes;
            try
            {
                hashedPasswordBytes = Convert.FromBase64String(hashedPassword);
            }
            catch (FormatException)
            {
                return false;
            }

            // Wrong length or version header.
            if (hashedPasswordBytes.Length != (1 + SaltSize + Pbkdf2SubkeyLength) || hashedPasswordBytes[0] != 0x00)
            {
                return false;
            }

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hashedPasswordBytes, 1, salt, 0, SaltSize);
            var storedSubkey = new byte[Pbkdf2SubkeyLength];
            Buffer.BlockCopy(hashedPasswordBytes, 1 + SaltSize, storedSubkey, 0, Pbkdf2SubkeyLength);

            var generatedSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2IterCount, HashAlgorithmName.SHA1, Pbkdf2SubkeyLength);
            return CryptographicOperations.FixedTimeEquals(storedSubkey, generatedSubkey);
        }
    }
}
