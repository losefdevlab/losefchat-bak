using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Collections.Generic;

namespace LosefDevLab.LosefChat.lcstd
{
    // Mod : Client, Des.: LC原版客户端核心类模组
    // Part : Client主部分
    public partial class Client
    {
        public TcpClient? tcpClient;
        public TcpClient? tcpClient2;
        public NetworkStream? clientStream;
        public string logFilePath = $"logclient.txt";
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
                tcpClient = new TcpClient();
                tcpClient2 = new TcpClient(AddressFamily.InterNetworkV6);

                if (ipvx == 4)
                {
                    Console.WriteLine("正在连接, 如您长时间看到这个界面, 则是要么是被封，要么是网络问题, 要么是密码防破解把你ban了。\n输入 'exit' 以关闭客户端。");
                    tcpClient.Connect(serverIP, serverPort);
                    clientStream = tcpClient.GetStream();
                }
                else if (ipvx == 6)
                {
                    Console.WriteLine("正在连接, 如您长时间看到这个界面, 则是要么是被封，要么是网络问题, 要么是密码防破解把你ban了。\n输入 'exit' 以关闭客户端。");
                    tcpClient2.Connect(serverIP, serverPort);
                    clientStream = tcpClient2.GetStream();
                }

                SendMessage(username);

                Thread.Sleep(100);//这一步是为了防止密码和用户名连在一起

                SendMessage(password);

                Console.WriteLine($"我({username})已连接到服务器。输入 'exit' 以关闭客户端。\n您的消息发送速度过快的话服务端可能会限制速度");
                Log($"我({username})已连接到服务器。");

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
    }
}