using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace LosefDevLab.LosefChat.lcstd
{
    // Mod : Server, Des.: LC原版服务端核心类模組
    // Part : Server主部分
    public partial class Server
    {
        public TcpListener? tcpListener;
        public List<ClientInfo> clientList = new List<ClientInfo>();
        public object lockObject = new object();
        public string searchFilePath = "search_results.txt";
        public string bannedUsersFilePath = "banned_users.txt";
        public string scacheFilePath = "outputcache.txt";
        public HashSet<string> bannedUsersSet;
        public HashSet<string> whiteListSet;
        private HashSet<string> _whiteListCache;
        private readonly object _whiteListLock = new object();


        private readonly object _cacheLock = new object();
        private readonly List<string> _logCache = new List<string>();
        private readonly Timer? _flushTimer;
        private int _messageTokens = 7200; // 每小时最大消息数,7.2km/h = 2m/s, m -> messages
        private readonly object _tokenLock = new object();
        private Timer? _tokenRefillTimer;

        private readonly Dictionary<string, string> userCredentials = new Dictionary<string, string>();

        public void CreateLCSFile()
        {
            if (!File.Exists(userFilePath))
            {
                using (File.Create(userFilePath)) { }
            }
            if (!File.Exists(pwdFilePath))
            {
                using (File.Create(pwdFilePath)) { }
            }
            if (!File.Exists(logFilePath))
            {
                using (File.Create(logFilePath)) { }
            }
            if (!File.Exists(searchFilePath))
            {
                using (File.Create(searchFilePath)) { }
            }

            if (!File.Exists(bannedUsersFilePath))
            {
                using (File.Create(bannedUsersFilePath)) { }
            }
        }

        public Server(int port)
        {
            Log($"Server port was set to {port}.");
            File.WriteAllText(scacheFilePath, string.Empty);
            CreateLCSFile();
            bannedUsersSet = File.ReadAllLines(bannedUsersFilePath).ToHashSet();
            Timer resetAttemptsTimer = new Timer(ResetLoginAttempts, null, TimeSpan.Zero, TimeSpan.FromDays(1));
            tcpListener = new TcpListener(IPAddress.Any, port);
            LoadUserCredentials();
            _tokenRefillTimer = new Timer(RefillTokens, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _flushTimer = new Timer(FlushCache, null, TimeSpan.Zero, TimeSpan.FromSeconds(0.5));
        }
        public void Stop()
        {
            tcpListener?.Stop();
            Log("服务器核心已关闭。");
            _flushTimer?.Dispose();
            _tokenRefillTimer?.Dispose();
        }

        public void Start()
        {
            Log("Server loading...");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            CreateLCSFile();

            if (tcpListener == null)
            {
                Log("TCP监听器未初始化。");
                return;
            }

            tcpListener.Start();
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Log($"Server started. [{elapsed.TotalMilliseconds} s]");

            Thread consoleInputThread = new Thread(new ThreadStart(ReadConsoleInput));
            consoleInputThread.Start();

            while (true)
            {
                try
                {
                    CreateLCSFile();

                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    tcpClient.ReceiveBufferSize = 65536;
                    tcpClient.SendBufferSize = 65536;

                    byte[] usernameBytes = new byte[65536];
                    int usernameBytesRead = tcpClient.GetStream().Read(usernameBytes, 0, 65536);
                    string username = Encoding.UTF8.GetString(usernameBytes, 0, usernameBytesRead);

                    byte[] passwordBytes = new byte[65536];
                    int passwordBytesRead = tcpClient.GetStream().Read(passwordBytes, 0, 65536);
                    string password = Encoding.UTF8.GetString(passwordBytes, 0, passwordBytesRead);

                    if (!IsUserValid(username, password))
                    {
                        Log($"拒绝了一个用户的连接请求: '{username}' 用户名或密码错误。");
                        tcpClient.Close();
                        continue;
                    }

                    ClientInfo clientInfo = new ClientInfo { TcpClient = tcpClient, Username = username };

                    lock (lockObject)
                    {
                        clientList.Add(clientInfo);
                    }

                    BroadcastMessage($"{clientInfo.Username} 加入了服务器");

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientCommunication));
                    clientThread.Start(clientInfo);
                }
                catch (Exception ex)
                {
                    Log($"接受客户端连接时发生异常: {ex.Message}");
                }
            }
        }

        public void HandleClientCommunication(object clientInfoObj)
        {
            try
            {
                if (clientInfoObj == null)
                {
                    throw new ArgumentNullException(nameof(clientInfoObj));
                }

                ClientInfo clientInfo = (ClientInfo)clientInfoObj;
                TcpClient tcpClient = clientInfo.TcpClient;
                if (bannedUsersSet.Contains(clientInfo.Username))
                {
                    Log($"用户 '{clientInfo.Username}' 被封禁, 无法连接。");
                    SendMessage(clientInfo, $"你 '{clientInfo.Username}' 被封禁, 无法连接。");
                    Thread.Sleep(500);
                    tcpClient?.Close();
                    return;
                }

                NetworkStream clientStream = tcpClient?.GetStream();
                if (clientStream == null || !tcpClient.Connected)
                {
                    Log($"无法获取网络流或客户端 '{clientInfo.Username}' 已断开。");
                    tcpClient?.Close();
                    return;
                }

                Log($"用户 '{clientInfo.Username}' 连接到服务器.");

                byte[] messageBytes = new byte[65134];
                int bytesRead;

                while (true)
                {
                    bytesRead = 0;

                    try
                    {
                        if (clientStream.DataAvailable)
                        {
                            bytesRead = clientStream.Read(messageBytes, 0, 65134);
                        }
                        else
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"读取用户 '{clientInfo.Username}' 的消息时发生异常: {ex.Message}");
                        break;
                    }

                    if (bytesRead == 0)
                        break;

                    string data = Encoding.UTF8.GetString(messageBytes, 0, bytesRead);

                    if (mutedUsersSet.Contains(clientInfo.Username))
                    {
                        SendMessage(clientInfo, "你已被禁言，无法发送消息！");
                        continue;
                    }
                    BroadcastMessage($"{clientInfo.Username}: {data}", clientInfo.Username);
                }

                lock (lockObject)
                {
                    clientList.Remove(clientInfo);
                }
                BroadcastMessage($"{clientInfo.Username} 下线。");
                tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Log($"处理用户通信时发生异常: {ex.Message}");
            }
        }

        public void BroadcastMessage(string message, string senderUsername = "")
        {
            if (message.Trim() == "")
            {
                return;
            }

            bool canSendMessage = false;
            lock (_tokenLock)
            {
                if (_messageTokens > 0)
                {
                    _messageTokens--;
                    canSendMessage = true;
                }
            }

            if (canSendMessage)
            {
                byte[] broadcastBytes = Encoding.UTF8.GetBytes(message);

                lock (lockObject)
                {
                    foreach (var client in clientList.ToList())
                    {
                        if (client.Username == senderUsername && mutedUsersSet.Contains(senderUsername))
                        {
                            continue;
                        }

                        try
                        {
                            if (client.TcpClient != null && client.TcpClient.Connected)
                            {
                                NetworkStream clientStream = client.TcpClient.GetStream();
                                if (clientStream.CanWrite)
                                {
                                    clientStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"广播消息到用户 {client.Username} 时发生异常: {ex.Message}");
                            clientList.Remove(client);
                            client.TcpClient?.Close();
                        }
                    }
                }

                Log(message);
            }
        }

        public void SendMessage(ClientInfo clientInfo, string message)
        {
            if (message.Trim() != "")
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                clientInfo.TcpClient.GetStream().Write(messageBytes, 0, messageBytes.Length);
                clientInfo.TcpClient.GetStream().Flush();
            }
            else
            { // nothing to do 
            }
        }

        public void BanUser(string targetUsername)
        {
            try
            {
                lock (lockObject)
                {
                    if (!bannedUsersSet.Contains(targetUsername))
                    {
                        bannedUsersSet.Add(targetUsername);
                        File.WriteAllLines(bannedUsersFilePath, bannedUsersSet);

                        Console.WriteLine($"用户 '{targetUsername}' 已被封禁.");
                        KickBannedUser(targetUsername);
                        Log($"用户 '{targetUsername}' 已被服务器封禁.");
                    }
                    else
                    {
                        Console.WriteLine($"用户 '{targetUsername}' 已经被封了,不要重复BAN哦.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"封禁用户时发生异常: {ex}");
                Log($"封禁用户时发生了异常: {ex}");
            }
        }

        public void KickBannedUser(string targetUsername)
        {
            try
            {
                lock (lockObject)
                {
                    ClientInfo? targetClient = clientList.FirstOrDefault(c => c.Username == targetUsername);

                    if (targetClient != null)
                    {
                        SendMessage(targetClient, "你被封了!");

                        BroadcastMessage($"用户 '{targetUsername}' 被服务器封禁");
                        clientList.Remove(targetClient);

                        Thread.Sleep(3000);
                        targetClient.TcpClient.Close();
                    }
                    else
                    {
                        Log($"虽然没有踢出 '{targetUsername}'(可能不在线), 但是我们封禁了, 下一次他进不来.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在踢出用户的时候发生异常: {ex}");
                Log($"在踢出用户的时候发生异常: {ex}");
            }
        }

        public void KickUser(string targetUsername)
        {
            try
            {
                lock (lockObject)
                {
                    ClientInfo? targetClient = clientList.FirstOrDefault(c => c.Username == targetUsername);

                    if (targetClient != null)
                    {
                        SendMessage(targetClient, "你被管理员踢了");
                        BroadcastMessage($"用户 '{targetUsername}' 被管理员踢了");
                        clientList.Remove(targetClient);
                        targetClient.TcpClient.Close();
                    }
                    else
                    {
                        Log($"'{targetUsername}' 这人我寻思着也不在线啊怎么踢？");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在踢出用户的时候发生异常: {ex}");
                Log($"在踢出用户的时候发生异常: {ex}");
            }
        }

        public void UnbanUser(string targetUsername)
        {
            try
            {
                lock (lockObject)
                {
                    if (bannedUsersSet.Contains(targetUsername))
                    {
                        bannedUsersSet.Remove(targetUsername);
                        File.WriteAllLines(bannedUsersFilePath, bannedUsersSet);
                        Console.WriteLine($"'{targetUsername}' 出狱了");
                        Log($"'{targetUsername}' 经服务器官方批准,出狱");
                    }
                    else
                    {
                        Console.WriteLine($"'{targetUsername}' 本来就不是被封禁用户 你这让我很为难啊qwq.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在解封用户时发生异常 {ex}");
                Log($"在解封用户时发生异常 {ex}");
            }
        }

        public void DisplayAllUsers()
        {
            try
            {
                lock (lockObject)
                {
                    Console.WriteLine("当前在线用户:");
                    foreach (var client in clientList)
                    {
                        Console.WriteLine("    " + client.Username);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在投影当前在线用户的时候发生异常 {ex}");
                Log($"在投影当前在线用户的时候发生异常 {ex}");
            }
        }
        public void ReadConsoleInput()
        {
            while (true)
            {
                string input = Console.ReadLine();

                if (input.StartsWith("l"))
                {
                    Console.Clear();
                    if (File.Exists(scacheFilePath))
                    {
                        string[] cacheContent = File.ReadAllLines(scacheFilePath);
                        foreach (var log in cacheContent)
                        {
                            Console.WriteLine(log);
                        }
                    }
                    else
                    {
                        Console.WriteLine("缓存文件不存在。");
                    }
                }
                else if (input.StartsWith("dl"))
                {
                    if (File.Exists(scacheFilePath))
                    {
                        File.WriteAllText(scacheFilePath, string.Empty);
                        Console.Clear();
                        Console.WriteLine("日志缓存已清空。");
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("缓存文件不存在。");
                    }
                }
                else if (input.StartsWith("/kick"))
                {
                    string targetUsername = input.Split(' ')[1];
                    KickUser(targetUsername);
                }
                else if (input.StartsWith("/ban"))
                {
                    string targetUsername = input.Split(' ')[1];
                    BanUser(targetUsername);
                }
                else if (input.StartsWith("/unban"))
                {
                    string targetUsername = input.Split(' ')[1];
                    UnbanUser(targetUsername);
                }
                else if (input.StartsWith("/mute"))
                {
                    string targetUsername = input.Split(' ')[1];
                    MuteUser(targetUsername);
                }
                else if (input.StartsWith("/unmute"))
                {
                    string targetUsername = input.Split(' ')[1];
                    UnmuteUser(targetUsername);
                }
                else if (input.Trim() == "/users")
                {
                    DisplayAllUsers();
                }
                else if (input.StartsWith("/search"))
                {
                    string searchKeyword = input.Substring(8);
                    SearchLog(searchKeyword);
                }
                else if (input.Trim() == "/clear")
                {
                    Console.Clear();
                }
                else if (input.StartsWith("/log"))
                {
                    string log = input.Substring(5);
                    Log("[人工记录日志]:" + log);
                }
                else if (input.StartsWith("/exit"))
                {

                    Stop();
                    Log("服务器已退出.将在几秒后关闭程序.");
                    Thread.Sleep(2000);
                    _flushTimer?.Dispose();

                    Environment.Exit(0);
                }
                else if (input.StartsWith("/bc"))
                {
                    string _bcmsg = input.Substring(4);
                    BroadcastMessage($"服务器广播：{_bcmsg}");
                }
                else if (input.StartsWith("/help"))
                {
                    Console.WriteLine("欢迎使用LosefChat Server控制台.");
                    Console.WriteLine("可用命令:");
                    Console.WriteLine("/exit: 退出Server");
                    Console.WriteLine("l: [ACIO]显示日志缓存");
                    Console.WriteLine("dl: [ACIO]清空日志缓存");
                    Console.WriteLine("/kick <username>: 踢出用户");
                    Console.WriteLine("/ban <username>: 封禁用户");
                    Console.WriteLine("/unban <username>: 解封用户");
                    Console.WriteLine("/mute <username>: 禁言用户");
                    Console.WriteLine("/unmute <username>: 解禁言用户");
                    Console.WriteLine("/users: 显示所有用户");
                    Console.WriteLine("/search <keyword>: 搜索日志");
                    Console.WriteLine("/clear: 清空控制台");
                    Console.WriteLine("/log <message>: 手动的记录日志, 适用于某些事件的标记与记录");
                    Console.WriteLine("/bc <message>: 广播消息给所有用户");
                    Console.WriteLine("/help: 显示可用命令");
                }
                else if (input.Trim() == "")
                {
                    continue;
                }
                else
                {
                    Console.WriteLine("无效的命令。请输入/help查看可用命令。");
                }

            }
        }


        private readonly object _fileLock = new object();

        private void RefillTokens(object? state)
        {
            lock (_tokenLock)
            {

                _messageTokens = Math.Min(7200, _messageTokens + 2);
            }
        }



        private void LoadUserCredentials()
        {
            try
            {
                var userLines = File.ReadAllLines(userFilePath);
                var pwdLines = File.ReadAllLines(pwdFilePath);
                userCredentials.Clear();
                for (int i = 0; i < Math.Min(userLines.Length, pwdLines.Length); i++)
                {
                    if (!string.IsNullOrEmpty(userLines[i]) && !string.IsNullOrEmpty(pwdLines[i]))
                    {
                        userCredentials[userLines[i]] = pwdLines[i];
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"加载用户凭证时发生异常: {ex.Message}");
            }
        }
    }
}