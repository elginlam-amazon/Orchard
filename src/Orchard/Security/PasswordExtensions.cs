using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Orchard.Security
{
    public static class PasswordExtensions
    {
        public const string PBKDF2 = "PBKDF2";

        public static string ComputeHashBase64(string hashAlgorithmName, byte[] saltBytes, string password)
        {
            var combinedBytes = CombineSaltAndPassword(saltBytes, password);

            if (hashAlgorithmName == PBKDF2)
            {
                return HashPasswordPbkdf2(combinedBytes);
            }
            else
            {
                using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmName))
                {
                    return Convert.ToBase64String(hashAlgorithm.ComputeHash(combinedBytes));
                }
            }
        }

        public static byte[] CombineSaltAndPassword(byte[] saltBytes, string password)
        {
            var passwordBytes = Encoding.Unicode.GetBytes(password);
            return saltBytes.Concat(passwordBytes).ToArray();
        }

        private static string HashPasswordPbkdf2(byte[] combinedBytes)
        {
            const int iterCount = 1000;
            const int saltSize = 16;
            const int subkeyLength = 20;

            var salt = new byte[saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] subkey;
            using (var prf = new Rfc2898DeriveBytes(combinedBytes, salt, iterCount, HashAlgorithmName.SHA1))
            {
                subkey = prf.GetBytes(subkeyLength);
            }

            var outputBytes = new byte[1 + 4 + saltSize + subkeyLength];
            outputBytes[0] = 0x00;
            Buffer.BlockCopy(BitConverter.GetBytes(iterCount), 0, outputBytes, 1, 4);
            Buffer.BlockCopy(salt, 0, outputBytes, 5, saltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 5 + saltSize, subkeyLength);
            return Convert.ToBase64String(outputBytes);
        }
    }
}
