//你居然发现了这么个神秘的东西
//那么好, 帮我延续一下去tls的开发吧
//但请务必记得，让用户能够自由选择是否使用安全通讯

using System;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
namespace lcstd
{
    // Mod : 安全通讯, Des.: 给开发者提供的安全通讯轮子
    public static class SafeCommunication
    {
        private static X509Certificate GetServerCertificate()
        {
            // 这里需要加载你的服务器证书
            // 例如：
            return new X509Certificate("sfc.pfx", "kkko_pppo_ccco_lllo_mmmo_aaao" +
            "_bbbo_dddo_fffo_gggo_hhho_iioo_jjjo_kkko_lllo_mmmo_nnno_oooo_pppo_qqqo_rrro_ssso_ttto_uuuo_vvvo_wwwo_xxxo_yyyo_zzzo");
        }

        public static SslStream AuthenticateServer(TcpClient tcpClient)
        {
            SslStream sslStream = new SslStream(tcpClient.GetStream(), false);
            sslStream.AuthenticateAsServer(GetServerCertificate(), false, System.Security.Authentication.SslProtocols.Tls12, true);
            return sslStream;
        }

        public static SslStream AuthenticateClient(TcpClient tcpClient, string serverIp)
        {
            SslStream sslStream = new SslStream(tcpClient.GetStream(), false);
            sslStream.AuthenticateAsClient(serverIp);
            return sslStream;
        }

        public static byte[] EncryptMessage(string message)
        {
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
            byte[] messageBytes = EncryptMessage(message);
            sslStream.Write(messageBytes, 0, messageBytes.Length);
            sslStream.Flush();
        }

        public static string ReceiveMessage(SslStream sslStream)
        {
            byte[] buffer = new byte[32567];
            int bytesRead = sslStream.Read(buffer, 0, 32567);
            return DecryptMessage(buffer, bytesRead);
        }
    }
}