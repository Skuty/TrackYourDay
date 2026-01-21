using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace TrackYourDay.Core
{
    public interface IEncryptionService
    {
        string Encrypt(string text);

        string Decrypt(string text);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly string salt;

        public EncryptionService()
        {
            this.salt = this.GetWindowsUserAccountId();
        }

        public EncryptionService(string salt)
        {
                this.salt = salt;
        }

        public string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            using (Aes aesAlg = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(salt, Encoding.UTF8.GetBytes(salt), 100000, HashAlgorithmName.SHA256).GetBytes(32);
                aesAlg.Key = key;
                aesAlg.GenerateIV();

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(text);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string Decrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            using (Aes aesAlg = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(salt, Encoding.UTF8.GetBytes(salt), 100000, HashAlgorithmName.SHA256).GetBytes(32);
                aesAlg.Key = key;
                using (var msDecrypt = new MemoryStream(Convert.FromBase64String(text)))
                {
                    byte[] ivLengthBytes = new byte[sizeof(int)];
                    msDecrypt.Read(ivLengthBytes, 0, ivLengthBytes.Length);
                    var ivLength = BitConverter.ToInt32(ivLengthBytes, 0);
                    var iv = new byte[ivLength];
                    msDecrypt.Read(iv, 0, iv.Length);
                    aesAlg.IV = iv;
                    var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        private string? GetWindowsUserAccountId()
        {
            var sid = WindowsIdentity.GetCurrent().User;

            if (sid == null)
            {
                return null;
            }

            return sid.Value;
        }

    }
}