using System.Security.Cryptography;

namespace Encryption
{
    public interface IEncryptionService
    {
        byte[] GenerateKey();
        byte[] GenerateIV();
        byte[] EncryptData(byte[] data, byte[] key, byte[] iv);
        byte[] DecryptData(byte[] encryptedData, byte[] key, byte[] iv);
    }


    public class EncryptionHelper : IEncryptionService
    {
        private const int KeySize = 128;
        private const int BlockSize = 128;

        public byte[] GenerateKey()
        {
            using Aes aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.GenerateKey();
            return aes.Key;
        }

        public byte[] GenerateIV()
        {
            using Aes aes = Aes.Create();
            aes.BlockSize = BlockSize;
            aes.GenerateIV();
            return aes.IV;
        }

        public byte[] EncryptData(byte[] data, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7; // Explicitly set padding mode
            using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using MemoryStream ms = new MemoryStream();
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock(); // Ensure final block is processed
            }
            return ms.ToArray();
        }

        public byte[] DecryptData(byte[] encryptedData, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7; // Explicitly set padding mode
            using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using MemoryStream ms = new MemoryStream(encryptedData);
            using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using MemoryStream decryptedStream = new MemoryStream();
            cs.CopyTo(decryptedStream);
            return decryptedStream.ToArray();
        }
    }

}
