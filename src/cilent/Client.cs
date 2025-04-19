using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Generic;

namespace lcstd
{
    public partial class Client
    {
        public TcpClient? tcpClient;
        public TcpClient? tcpClient2;
        public NetworkStream? clientStream;
        public string logFilePath = "logclient.txt";
        public string cacheFilePath = "catch.txt";
        public StreamWriter logFile;
        public StreamWriter cacheFile;
        public string usernamecpy = "";
        private List<string> messageBuffer = new List<string>();

        public Client(int other)
        {
            logFilePath = $"logclient{other}.txt";
            // 初始化日志文件
            if (!File.Exists(logFilePath))
            {
                using (File.Create(logFilePath)) { }
            }
            logFile = new StreamWriter(logFilePath, true);

            // 初始化缓存文件
            if (!File.Exists(cacheFilePath))
            {
                using (File.Create(cacheFilePath)) { }
            }
            cacheFile = new StreamWriter(cacheFilePath, true);
        }

        ~Client()
        {
            logFile?.Close();
            cacheFile?.Close();
        }

        public void Log(string message)
        {
            logFile.WriteLine($"{DateTime.Now}: {message}");
            logFile.Flush();
        }

        public void CacheMessage(string message)
        {
            cacheFile.WriteLine($"{DateTime.Now}: {message}");
            cacheFile.Flush();
            messageBuffer.Add(message);
            
            // 异步播放提示音
            ThreadPool.QueueUserWorkItem(_ => Console.Beep());
        }

        public void ClearCache()
        {
            cacheFile?.Close();
            File.WriteAllText(cacheFilePath, string.Empty);
            cacheFile = new StreamWriter(cacheFilePath, true);
            messageBuffer.Clear();
        }

        public void Connect(int ipvx, string serverIP, int serverPort, string username, string password)
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient2 = new TcpClient(AddressFamily.InterNetworkV6);

                if (ipvx == 4)
                {
                    tcpClient.Connect(serverIP, serverPort);
                    clientStream = tcpClient.GetStream();
                }
                else if (ipvx == 6)
                {
                    tcpClient2.Connect(serverIP, serverPort);
                    clientStream = tcpClient2.GetStream();
                }

                // 发送用户名和密码
                SendMessage(username);
                Thread.Sleep(100);
                SendMessage(password);

                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                receiveThread.Start();

                Console.WriteLine("正在连接，如长时间看到此界面，可能是被封禁、网络问题或密码错误。");
                Console.WriteLine("按 F2 显示缓存的消息");

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.F2)
                        {
                            // 显示并清空缓存
                            Console.Clear();
                            foreach (var msg in messageBuffer)
                            {
                                Console.WriteLine($"{DateTime.Now} > {msg}");
                            }
                            ClearCache();
                            Console.WriteLine("已显示缓存消息。新消息将继续存入缓存。");
                        }
                        else if (key.Key == ConsoleKey.F9)
                        {
                            // 屏蔽F9，至于这么做的原因是有人会用CapsWriter语音输入程序,当设置为特定键的时候，就会让终端搞出一些花里胡哨的东西，影响使用体验
                            // 我们想要的是CapsWriter的语音输入, 而不是这些花里胡哨的东西
                            // 所以我们必须屏蔽f9键，以防止影响使用体验
                            // 这里所用的一种方案就是忽略
                            // 但是由于按下f9键然后f9键不会忽略，但是语音输入并不会忽略F9,所以语音输入仍会生效
                            continue;
                        }
                    }

                    string message = Console.ReadLine();

                    if (message.ToLower() == "exit")
                    {
                        SendMessage("我下线了啊拜拜");
                        tcpClient?.Close();
                        break;
                    }

                    SendMessage(message);
                }
            }
            catch (Exception ex)
            {
                Log($"连接服务器时发生异常: {ex.Message}");
            }
        }

        public void ReceiveMessage()
        {
            byte[] message = new byte[32567];
            int bytesRead;

            bool connectionMessageShown = false;

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
                string logtmp = $"{DateTime.Now} > {data}";
                Log(logtmp);
                CacheMessage(data);

                if (!connectionMessageShown)
                {
                    Log($"我({usernamecpy})已连接到服务器。输入 'exit' 以关闭客户端。");
                    connectionMessageShown = true;
                }
            }
        }

        public void SendMessage(string message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            clientStream?.Write(messageBytes, 0, messageBytes.Length);
            clientStream?.Flush();
        }

        // Mod开发区域保持不变
        public class mod
        {
            public Client clientcpy;
            public mod(Client client)
            {
                clientcpy = client;
            }
            public string Name {
                get;
                set;
            } = null!;
            public string Description {
                get;
                set;
            } = null!;
            public void Start()
            {
                // Console.WriteLine();
            }
        }
    }
}