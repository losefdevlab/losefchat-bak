using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace LosefDevLab.LosefChat.lcstd
{
    // Mod : Client, Des.: LC原版客户端核心类模组
    // Part : Client主部分
    public partial class Client
    {
        public TcpClient? tcpClient;
        public TcpClient? tcpClient2;
        public NetworkStream? clientStream;
        public string logFilePath = $"logclient{DateTime.Now:yyyy}{DateTime.Now:MM}{DateTime.Now:dd}.txt";
        public StreamWriter logFile;
        public string usernamecpy = "";
        private string inputFilePath = ".ci";

        public Client()
        {
            if (!File.Exists(logFilePath))
            {
                using (File.Create(logFilePath)) { }
            }
            if (!File.Exists(inputFilePath))
            {
                using (File.Create(inputFilePath)) { }
            }
            else
            {
                File.WriteAllText(inputFilePath, string.Empty);
            }
            logFile = new StreamWriter(logFilePath, true);
        }

        ~Client()
        {
            // 关闭 StreamWriter
            logFile?.Close();
        }

        public void Log(string message)
        {
            if (message.Trim() != "")
            {
                logFile.WriteLine($"{DateTime.Now}: {message}");
                logFile.Flush();
            }
            else
            {
                // nothing to do
            }
        }

        public void Connect(int ipvx, string serverIP, int serverPort, string username, string password)
        {
            try
            {
                if (ipvx == 4)
                {
                    Console.WriteLine("正在连接, 如您长时间看到这个界面, 则是要么是被封，要么是网络问题, 要么是密码防破解把你ban了。输入 'exit' 以关闭客户端。");
                    Log($"[Connect] 尝试通过 IPv4 连接{serverIP}:{serverPort}");
                    tcpClient = new TcpClient();
                    tcpClient.Connect(serverIP, serverPort);
                    clientStream = tcpClient.GetStream();

                    // 检查是否成功连接
                    if (tcpClient.Connected)
                    {
                        Log($"[Connect] 成功通过 IPv4 连接到服务器{serverIP}:{serverPort}");
                    }
                    else
                    {
                        try
                        {
                            Log($"[Connect Finished unsuccessfully] 通过 IPv4 连接服务器失败");
                            throw new Exception("IPv4 连接失败");
                        }
                        catch (Exception ex)
                        {
                            Log($"[Connect Finished unsuccessfully] 通过 IPv4 连接服务器失败:  {ex.Message}");
                            throw new Exception("IPv4 连接失败", ex);
                        }
                    }
                }
                else if (ipvx == 6)
                {
                    Console.WriteLine("正在连接, 如您长时间看到这个界面, 则是要么是被封，要么是网络问题, 要么是密码防破解把你ban了。输入 'exit' 以关闭客户端。");
                    Log($"[Connect] 尝试通过 IPv6 连接{serverIP}:{serverPort}");
                    tcpClient2 = new TcpClient(AddressFamily.InterNetworkV6);
                    tcpClient2.Connect(serverIP, serverPort);
                    clientStream = tcpClient2.GetStream();

                    // 检查是否成功连接
                    if (tcpClient2.Connected)
                    {
                        Log($"[Connect] 成功通过 IPv6 连接到服务器{serverIP}:{serverPort}");
                    }
                    else
                    {
                        Log($"[Connect Finished unsuccessfully] 通过 IPv6 连接服务器失败{serverIP}:{serverPort}");
                        throw new Exception("IPv6 连接失败");
                    }
                }

                byte[] challengeBytes = new byte[256];
                int bytesRead = clientStream.Read(challengeBytes, 0, challengeBytes.Length);
                string challenge = Encoding.UTF8.GetString(challengeBytes, 0, bytesRead).Trim();

                if (challenge.StartsWith("[CHALLENGE]"))
                {
                    Log($"[Prove] 服务端{serverIP}:{serverPort}将会验证你现在使用的客户端是否为他们定义的非法客户端");
                    string nonce = challenge.Replace("[CHALLENGE]", "").Trim();
                    string secretKey = "losefchat-client-secret-key";// 这里需要和服务端的对应证明客户端不非法的私钥一模一样
                    string response = ComputeSHA256Hash(nonce + secretKey);

                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    clientStream.Write(responseBytes, 0, responseBytes.Length);
                    clientStream.Flush();

                    byte[] authResult = new byte[1024];
                    bytesRead = clientStream.Read(authResult, 0, authResult.Length);
                    string authStatus = Encoding.UTF8.GetString(authResult, 0, bytesRead).Trim();

                    if (authStatus != "[AUTH:OK]")
                    {
                        Console.WriteLine("验证失败：服务端认为你是非法客户端，如果你这个并不是非法客户端，请看看有什么其他地方会让服务端识别你为非法客户端");
                        Log($"[Prove Finished unsuccessfully] 验证失败：服务端认为你是非法客户端，如果你这个并不是非法客户端，请看看有什么其他地方会让服务端识别你为非法客户端");
                        tcpClient?.Close();
                        tcpClient2?.Close();
                        return;
                    }
                    Log($"[Prove Finished successfully] 证明成功!");
                }

                SendMessage(username);
                Log($"[Connect] 已发送用户名");

                Thread.Sleep(100);

                SendMessage(password);
                Log($"[Connect] 已发送密码");

                Console.WriteLine($"我({username})已加入到服务器({serverIP}:{serverPort})。输入 'exit' 以关闭客户端。\n您的消息发送速度过快的话服务端可能会限制速度");
                Log($"[Connect Finished successfully]我({username})已加入到服务器。server:{serverIP}:{serverPort}");

                ThreadPool.QueueUserWorkItem(state => ReceiveMessage());
                ThreadPool.QueueUserWorkItem(state => ProcessInput());
            }
            catch (Exception ex)
            {
                Log($"连接服务器时发生异常: {ex.Message}");
            }
        }

        private void ProcessInput()
        {
            while (true)
            {
                using (FileStream fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string msg = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        if (msg.Trim() != "" && msg != "exit") SendMessage(msg);
                        else if (msg.Trim() == "exit") { SendMessage("我下线了拜拜"); Environment.Exit(0); }
                        using (FileStream fileStreamWrite = new FileStream(inputFilePath, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
                        {
                        }
                    }
                }

                Thread.Sleep(10);
            }
        }

        public void ReceiveMessage()
        {
            byte[] message = new byte[32567];
            int bytesRead;

            List<string> messages = new List<string>();
            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream?.Read(message, 0, 32567) ?? 0;
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                string data = Encoding.UTF8.GetString(message, 0, bytesRead);
                messages.Add($"\a{DateTime.Now} > {data}");
                string logtmp = $"{DateTime.Now} > {data}";
                Log(logtmp);
                Console.WriteLine(logtmp);
            }
        }

        public void SendMessage(string message)
        {
            if (message.Trim() != "")
            {
                if (message == null) throw new ArgumentNullException(nameof(message));

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                clientStream?.Write(messageBytes, 0, messageBytes.Length);
                clientStream?.Flush();
            }
        }

        private string ComputeSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}