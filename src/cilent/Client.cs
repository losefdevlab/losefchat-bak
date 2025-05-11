using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace LosefChat.Client
{
    public class Client
    {
        private TcpClient tcpClient;
        private TcpClient tcpClient2;
        private NetworkStream clientStream;
        private string logFilePath = "client_log.txt";
        private StreamWriter logFile;

        public Client()
        {
            if (!File.Exists(logFilePath))
            {
                using (File.Create(logFilePath)) { }
            }
            if (!File.Exists("input.txt"))
            {
                using (File.Create("input.txt")) { }
            }
            else
            {
                File.WriteAllText("input.txt", string.Empty);
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

                    // 确保连接后检查连接状态
                    if (!tcpClient.Connected)
                    {
                        Console.WriteLine("连接已中断，请重新尝试连接。");
                        return;
                    }

                    clientStream = tcpClient.GetStream();
                }
                else if (ipvx == 6)
                {
                    Console.WriteLine("正在连接, 如您长时间看到这个界面, 则是要么是被封，要么是网络问题, 要么是密码防破解把你ban了。\n输入 'exit' 以关闭客户端。");
                    tcpClient2.Connect(serverIP, serverPort);

                    // 确保连接后检查连接状态
                    if (!tcpClient2.Connected)
                    {
                        Console.WriteLine("连接已中断，请重新尝试连接。");
                        return;
                    }

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
                Console.WriteLine($"连接失败: {ex.Message}. 请检查网络或服务器状态。");
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
                    // 检查连接状态
                    if (clientStream == null ||
                        (tcpClient != null && !tcpClient.Connected) ||
                        (tcpClient2 != null && !tcpClient2.Connected))
                    {
                        break;
                    }

                    bytesRead = clientStream.Read(message, 0, 32567);
                }
                catch (Exception ex)
                {
                    Log($"接收消息时发生异常: {ex.Message}");
                    Console.WriteLine($"连接已中断: {ex.Message}");
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

            // 清理资源
            Cleanup();
        }

        private void Cleanup()
        {
            try
            {
                clientStream?.Close();
            }
            catch { }

            try
            {
                tcpClient?.Close();
            }
            catch { }

            try
            {
                tcpClient2?.Close();
            }
            catch { }

            Console.WriteLine("已与服务器断开连接。");
        }

        public void SendMessage(string message)
        {
            if (message.Trim() != "")
            {
                if (message == null) throw new ArgumentNullException(nameof(message));

                try
                {
                    // 检查连接状态
                    if (clientStream == null ||
                        (tcpClient != null && !tcpClient.Connected) ||
                        (tcpClient2 != null && !tcpClient2.Connected))
                    {
                        Console.WriteLine("无法发送消息: 与服务器的连接已断开。");
                        return;
                    }

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    clientStream?.Write(messageBytes, 0, messageBytes.Length);
                    clientStream?.Flush();
                }
                catch (Exception ex)
                {
                    Log($"发送消息时发生异常: {ex.Message}");
                    Console.WriteLine($"消息发送失败: {ex.Message}");
                }
            }
        }
    }