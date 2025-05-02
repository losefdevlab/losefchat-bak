//你居然发现了这么个神秘的东西
//那么好, 帮我延续一下去tls的开发吧
//但请务必记得，让用户能够自由选择是否使用安全通讯

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace lcstd
{
    // Mod : 安全通讯, Des.: 给开发者提供的安全通讯轮子
    public static class SafeCommunication
    {
        private static X509Certificate GetServerCertificate()
        {
            // 这里需要加载你的服务器证书
            return new X509Certificate("sfc.pfx", "kkko_pppo_ccco_lllo_mmmo_aaao" +
            "_bbbo_dddo_fffo_gggo_hhho_iioo_jjjo_kkko_lllo_mmmo_nnno_oooo_pppo_qqqo_rrro_ssso_ttto_uuuo_vvvo_wwwo_xxxo_yyyo_zzzo");
        }

        public static SslStream AuthenticateServer(TcpClient tcpClient)
        {
            SslStream sslStream = new SslStream(tcpClient.GetStream(), false);
            sslStream.AuthenticateAsServer(GetServerCertificate(), false, SslProtocols.Tls13, true);
            return sslStream;
        }

        public static SslStream AuthenticateClient(TcpClient tcpClient, string serverIp)
        {
            SslStream sslStream = new SslStream(tcpClient.GetStream(), false);
            sslStream.AuthenticateAsClient(serverIp, null, SslProtocols.Tls13, false);
            return sslStream;
        }

        // TLS握手后自定义密钥交换
        public static byte[] ExchangeCustomKey(SslStream sslStream)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                string publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
                byte[] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
                sslStream.Write(publicKeyBytes, 0, publicKeyBytes.Length);

                byte[] response = new byte[2048];
                int bytesRead = sslStream.Read(response, 0, response.Length);
                if (bytesRead > 0)
                {
                    string encryptedKey = Encoding.UTF8.GetString(response, 0, bytesRead);
                    try
                    {
                        return rsa.Decrypt(Convert.FromBase64String(encryptedKey), RSAEncryptionPadding.OaepSHA256);
                    }
                    catch (CryptographicException)
                    {
                        // 处理解密失败的情况
                        return null;
                    }
                }
                return null;
            }
        }

        public static Aes CreateAesCbcAlgorithm()
        {
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.GenerateIV();
            return aes;
        }

        public static byte[] GenerateEphemeralKey()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                ECDiffieHellmanPublicKey remotePublicKey = ecdh.PublicKey;
                return ecdh.DeriveKeyMaterial(remotePublicKey);
            }
        }
        public static byte[] AddObfuscationLayer(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return data;
            }

            Random random = new Random();
            byte[] padding = new byte[random.Next(16, 64)];
            random.NextBytes(padding);

            byte[] obfuscatedData = new byte[data.Length + padding.Length * 2];
            Buffer.BlockCopy(padding, 0, obfuscatedData, 0, padding.Length);
            Buffer.BlockCopy(data, 0, obfuscatedData, padding.Length, data.Length);
            Buffer.BlockCopy(padding, 0, obfuscatedData, padding.Length + data.Length, padding.Length);

            return obfuscatedData;
        }

        public static byte[] EncryptMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] encryptedBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(messageBytes, 0, messageBytes.Length);
                    }
                    encryptedBytes = ms.ToArray();
                }

                byte[] keyIVConcatenated = new byte[aes.Key.Length + aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.Key, 0, keyIVConcatenated, 0, aes.Key.Length);
                Buffer.BlockCopy(aes.IV, 0, keyIVConcatenated, aes.Key.Length, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, keyIVConcatenated, aes.Key.Length + aes.IV.Length, encryptedBytes.Length);

                return keyIVConcatenated;
            }
        }

        public static string DecryptMessage(byte[] encryptedBytes, int length)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0 || length <= 0)
            {
                return string.Empty;
            }

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                byte[] key = new byte[aes.KeySize / 8];
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] messageBytes = new byte[length - aes.KeySize / 8 - aes.BlockSize / 8];

                Buffer.BlockCopy(encryptedBytes, 0, key, 0, aes.KeySize / 8);
                Buffer.BlockCopy(encryptedBytes, aes.KeySize / 8, iv, 0, aes.BlockSize / 8);
                Buffer.BlockCopy(encryptedBytes, aes.KeySize / 8 + aes.BlockSize / 8, messageBytes, 0, length - aes.KeySize / 8 - aes.BlockSize / 8);

                aes.Key = key;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(messageBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static void SendMessage(SslStream sslStream, string message)
        {
            if (sslStream == null || message == null)
            {
                return; // 参数验证
            }

            byte[] customKey = ExchangeCustomKey(sslStream);
            if (customKey == null)
            {
                return; // 密钥交换失败处理
            }

            Aes aes = CreateAesCbcAlgorithm();
            aes.Key = customKey;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] encryptedBytes = aes.EncryptCbc(messageBytes, aes.IV);

            byte[] ivAndEncrypted = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, ivAndEncrypted, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, ivAndEncrypted, aes.IV.Length, encryptedBytes.Length);

            byte[] obfuscatedData = AddObfuscationLayer(ivAndEncrypted);

            sslStream.Write(obfuscatedData, 0, obfuscatedData.Length);
            sslStream.Flush();
        }

        public static string ReceiveMessage(SslStream sslStream)
        {
            if (sslStream == null)
            {
                return string.Empty; // 参数验证
            }

            byte[] buffer = new byte[32567];
            int bytesRead = sslStream.Read(buffer, 0, 32567);
            if (bytesRead <= 0)
            {
                return string.Empty; // 无数据读取处理
            }

            byte[] receivedData = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, receivedData, 0, bytesRead);

            byte[] defuscatedData = RemoveObfuscationLayer(receivedData);
            if (defuscatedData == null || defuscatedData.Length == 0)
            {
                return string.Empty; // 混淆数据无效处理
            }

            byte[] iv = new byte[16];
            Buffer.BlockCopy(defuscatedData, 0, iv, 0, iv.Length);

            byte[] encryptedBytes = new byte[defuscatedData.Length - iv.Length];
            Buffer.BlockCopy(defuscatedData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using (Aes aes = CreateAesCbcAlgorithm())
            {
                byte[] customKey = ExchangeCustomKey(sslStream);
                if (customKey == null)
                {
                    return string.Empty;
                }

                aes.Key = customKey;

                byte[] decryptedBytes = aes.DecryptCbc(encryptedBytes, iv);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
        private static byte[] RemoveObfuscationLayer(byte[] obfuscatedData)
        {
            if (obfuscatedData == null || obfuscatedData.Length < 1)
            {
                return obfuscatedData;
            }

            int prefixPaddingLength = obfuscatedData[0];
            int suffixPaddingStart = obfuscatedData.Length - prefixPaddingLength;
            int dataLength = suffixPaddingStart - prefixPaddingLength;

            if (dataLength <= 0)
            {
                return new byte[0];
            }

            byte[] data = new byte[dataLength];
            Buffer.BlockCopy(obfuscatedData, prefixPaddingLength, data, 0, dataLength);

            return data;
        }
    }
}