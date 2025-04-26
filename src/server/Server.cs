using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Concurrent;

namespace lcstd
{
    // Mod : Server, Des.: LC原版服务端核心类模组
    // Part : Server主部分
    public partial class Server
    {
        public ConcurrentQueue<ClientInfo> clientQueue = new ConcurrentQueue<ClientInfo>();
        public TcpListener tcpListener;
        public List<ClientInfo> clientList = new List<ClientInfo>();
        public object lockObject = new object();
        public string logFilePath = "log.txt"; // Log file path
        public string searchFilePath = "search_results.txt"; // Search results file path
        public string bannedUsersFilePath = "banned_users.txt"; // Banned users file path
        public string whiteListFilePath = "white_list.txt"; // White list file path
        public string scacheFilePath = "outputcache.txt";
        public HashSet<string> bannedUsersSet;
        public HashSet<string> whiteListSet;
        public bool isServerUseTheWhiteList = false;// 白名单是否开启的选项,默认关闭

        private readonly object _cacheLock = new object();
        private readonly List<string> _logCache = new List<string>();
        private readonly Timer _flushTimer;

        private void EnsureFilesExist()
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
            if (!File.Exists(whiteListFilePath))
            {
                using (File.Create(whiteListFilePath)) { }
            }
            if (!File.Exists(scacheFilePath))
            {
                using (File.Create(scacheFilePath)) { }
            }
        }

        public Server(int port)
        {
            Log($"Server port was set to {port}.");
            EnsureFilesExist();

            bannedUsersSet = File.ReadAllLines(bannedUsersFilePath).ToHashSet();
            whiteListSet = File.ReadAllLines(whiteListFilePath).ToHashSet();
            Timer resetAttemptsTimer = new Timer(ResetLoginAttempts, null, TimeSpan.Zero, TimeSpan.FromDays(1));
            tcpListener = new TcpListener(IPAddress.Any, port);

            // 设置定时器，每10秒刷新一次缓存到文件
            _flushTimer = new Timer(FlushCache, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public void Start()
        {
            Log("Server loading...");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            EnsureFilesExist();

            tcpListener.Start();
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Log($"Server started. [{elapsed.TotalMilliseconds} s]");

            Thread consoleInputThread = new Thread(new ThreadStart(ReadConsoleInput));
            consoleInputThread.Start();

            int numberOfThreads = Environment.ProcessorCount;
            for (int i = 0; i < numberOfThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(HandleClientCommunication);
            }

            while (true)
            {
                EnsureFilesExist();
                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                byte[] usernameBytes = new byte[32567];
                int usernameBytesRead = tcpClient.GetStream().Read(usernameBytes, 0, 32567);
                string username = Encoding.UTF8.GetString(usernameBytes, 0, usernameBytesRead);

                byte[] passwordBytes = new byte[32567];
                int passwordBytesRead = tcpClient.GetStream().Read(passwordBytes, 0, 32567);
                string password = Encoding.UTF8.GetString(passwordBytes, 0, passwordBytesRead);

                if (!IsUserValid(username, password))
                {
                    Log($"拒绝了一个用户的连接请求: '{username}' 用户名或密码错误.");
                    tcpClient.Close();
                    continue;
                }

                ClientInfo clientInfo = new ClientInfo { TcpClient = tcpClient, Username = username };

                clientQueue.Enqueue(clientInfo);
            }
        }

        public void HandleClientCommunication(object state)
        {
            try
            {
                if (!clientQueue.TryDequeue(out ClientInfo clientInfo))
                {
                    return;
                }

                TcpClient tcpClient = clientInfo.TcpClient;

                // 封禁用户检测机制
                if (bannedUsersSet.Contains(clientInfo.Username))
                {
                    Log($"用户 '{clientInfo.Username}' 被封禁, 无法连接.");
                    SendMessage(clientInfo, $"你 '{clientInfo.Username}' 被封禁, 无法连接.");
                    Thread.Sleep(500);
                    tcpClient.Close();
                    return;
                }

                NetworkStream clientStream = tcpClient.GetStream();

                Log($"用户 '{clientInfo.Username}' 连接到服务器.");

                byte[] messageBytes = new byte[32567];
                int bytesRead;

                while (true)
                {
                    bytesRead = 0;

                    try
                    {
                        bytesRead = clientStream.Read(messageBytes, 0, 32567);
                    }
                    catch (Exception ex)
                    {
                        Log($"读取用户 '{clientInfo.Username}' 的消息时发生异常: {ex.Message}");
                        break;
                    }

                    if (bytesRead == 0)
                        break;

                    string data = Encoding.UTF8.GetString(messageBytes, 0, bytesRead);

                    // 检查用户是否被禁言
                    if (mutedUsersSet.Contains(clientInfo.Username))
                    {
                        // 忽略被禁言用户的消息
                        SendMessage(clientInfo, "你已被禁言，无法发送消息！");
                        continue;
                    }

                    // 广播消息到所有客户端
                    BroadcastMessage($"{clientInfo.Username}: {data}", clientInfo.Username);
                }

                lock (lockObject)
                {
                    clientList.Remove(clientInfo);
                }
                BroadcastMessage($"{clientInfo.Username} 下线.");
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Log($"处理用户通信时发生异常: {ex.Message}");
            }
        }
        public void BroadcastMessage(string message, string senderUsername = "")
        {
            if (message.Trim() != "")
            {
                byte[] broadcastBytes = Encoding.UTF8.GetBytes(message);

                lock (lockObject)
                {
                    foreach (var client in clientList.ToList())
                    {
                        try
                        {
                            // 如果发送者是被禁言用户，则跳过广播其消息
                            if (client.Username == senderUsername && mutedUsersSet.Contains(senderUsername))
                            {
                                continue;
                            }

                            NetworkStream clientStream = client.TcpClient.GetStream();
                            clientStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                            clientStream.Flush();
                        }
                        catch (Exception ex)
                        {
                            Log($"广播消息给用户 '{client.Username}' 时发生异常: {ex.Message}");
                        }
                    }
                }

                // 记录广播消息到日志文件
                Log(message);
            }
        }

        public void SendMessage(ClientInfo clientInfo, string message)
        {
            if (message != "" || message.Trim() != "")
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

                        // Update banned users file
                        File.WriteAllLines(bannedUsersFilePath, bannedUsersSet);

                        Console.WriteLine($"用户 '{targetUsername}' 已被封禁.");

                        // Try to kick out the banned user
                        KickBannedUser(targetUsername);

                        // Log ban operation to the log file
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
                    ClientInfo targetClient = clientList.FirstOrDefault(c => c.Username == targetUsername);

                    if (targetClient != null)
                    {
                        SendMessage(targetClient, "你被封了!");

                        // Send kicked out message to other users
                        BroadcastMessage($"用户 '{targetUsername}' 被服务器封禁");

                        // Remove the kicked out user from the client list
                        clientList.Remove(targetClient);

                        Thread.Sleep(3000);

                        // Close the connection with the kicked out user
                        targetClient.TcpClient.Close();
                    }
                    else
                    {
                        // If user does not exist, log invalid user message to the log file
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
                    ClientInfo targetClient = clientList.FirstOrDefault(c => c.Username == targetUsername);

                    if (targetClient != null)
                    {
                        SendMessage(targetClient, "你被管理员踢了");

                        // Send kicked out message to other users
                        BroadcastMessage($"用户 '{targetUsername}' 被管理员踢了");

                        // Remove the kicked out user from the client list
                        clientList.Remove(targetClient);

                        // Close the connection with the kicked out user
                        targetClient.TcpClient.Close();
                    }
                    else
                    {
                        // If user does not exist, log invalid user message to the log file
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

                        // Update banned users file
                        File.WriteAllLines(bannedUsersFilePath, bannedUsersSet);

                        Console.WriteLine($"'{targetUsername}' 出狱了");

                        // Log unban operation to the log file
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

        public void DisplayAllUsers()//显示所有用户
        {
            try
            {
                lock (lockObject)
                {
                    Console.WriteLine("当前在线用户:");
                    foreach (var client in clientList)
                    {
                        Console.WriteLine("    " + client.Username);//在这里遍历然后显示
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在投影当前在线用户的时候发生异常 {ex}");
                Log($"在投影当前在线用户的时候发生异常 {ex}");
            }//我还是太谨慎了这玩意可能永远都不会触发
        }

        public void AddToWhiteList(string username)
        {
            try
            {
                lock (lockObject)
                {
                    if (!whiteListSet.Contains(username))
                    {
                        whiteListSet.Add(username);
                        File.WriteAllLines(whiteListFilePath, whiteListSet);

                        Console.WriteLine($"'{username}' 已添加到白名单.");
                        Log($"'{username}' 已添加到白名单.");
                    }
                    else
                    {
                        Console.WriteLine($"'{username}' 已经在白名单里面了,不要重复添加.");
                        Log($"'{username}' 已经在白名单里面了,不要重复添加.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在添加到白名单时发生异常 {ex}");
                Log($"在添加到白名单时发生异常 {ex}");
            }
        }

        public void RemoveFromWhiteList(string username)
        {
            try
            {
                lock (lockObject)
                {
                    if (whiteListSet.Contains(username))
                    {
                        whiteListSet.Remove(username);
                        File.WriteAllLines(whiteListFilePath, whiteListSet);

                        Console.WriteLine($"'{username}' 已从白名单中移除.");
                        Log($"'{username}' 已从白名单中移除.");
                    }
                    else
                    {
                        Console.WriteLine($"'{username}' 本来就不在白名单里面,不要重复移除.");
                        Log($"'{username}' 本来就不在白名单里面,不要重复移除.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在从白名单中移除用户时发生异常 {ex}");
                Log($"在从白名单中移除用户时发生异常 {ex}");
            }
        }

        public void LetWhiteListUserJoin()
        {
            try
            {
                lock (lockObject)
                {
                    foreach (var client in clientList)
                    {
                        if (whiteListSet.Contains(client.Username))
                        {
                            Log(client.Username + " 被添加到了白名单,可以加入服务器了!");
                        }
                    }

                }
            }
            catch (Exception ex)
            {

                Log($"在允许白名单用户加入时发生异常 {ex}");
            }
        }

        public void ReadConsoleInput()
        {
            while (true)
            {
                string input = Console.ReadLine();

                if (input.StartsWith("/l"))
                {
                    Console.Clear();
                    Console.WriteLine("缓存中的日志:");
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
                else if (input.StartsWith("/dl"))
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
                else if (input.StartsWith("/mute")) // 禁言命令
                {
                    string targetUsername = input.Split(' ')[1];
                    MuteUser(targetUsername);
                }
                else if (input.StartsWith("/unmute")) // 解禁言命令
                {
                    string targetUsername = input.Split(' ')[1];
                    UnmuteUser(targetUsername);
                }
                else if (input.StartsWith("/users"))
                {
                    DisplayAllUsers();
                }
                else if (input.StartsWith("/search"))
                {
                    string searchKeyword = input.Substring(8);
                    SearchLog(searchKeyword);
                }
                else if (input.StartsWith("/addwl"))
                {
                    string username = input.Substring(10);
                    AddToWhiteList(username);
                }
                else if (input.StartsWith("/rmwl"))
                {
                    string username = input.Substring(14);
                    RemoveFromWhiteList(username);
                }
                else if (input.StartsWith("/usewl"))
                {
                    isServerUseTheWhiteList = true;
                    Log("服务器已启用白名单模式.");
                }
                else if (input.StartsWith("/notwl"))
                {
                    isServerUseTheWhiteList = false;
                    Log("服务器已关闭白名单模式.");
                }
                else if (input.StartsWith("/clear"))
                {
                    Console.Clear();
                }
                else if (input.StartsWith("/help"))
                {
                    Console.WriteLine("可用命令:");
                    Console.WriteLine("/l: 显示日志缓存");
                    Console.WriteLine("/dl: 清空日志缓存");
                    Console.WriteLine("/kick <username>: 踢出用户");
                    Console.WriteLine("/ban <username>: 封禁用户");
                    Console.WriteLine("/unban <username>: 解封用户");
                    Console.WriteLine("/mute <username>: 禁言用户");
                    Console.WriteLine("/unmute <username>: 解禁言用户");
                    Console.WriteLine("/users: 显示所有用户");
                    Console.WriteLine("/search <keyword>: 搜索日志");
                    Console.WriteLine("/addwl <username>: 将用户添加到白名单");
                    Console.WriteLine("/rmwl <username>: 从白名单中移除用户");
                    Console.WriteLine("/usewl: 启用白名单模式");
                    Console.WriteLine("/notwl: 关闭白名单模式");
                    Console.WriteLine("/clear: 清空控制台");
                    Console.WriteLine("/help: 显示可用命令");
                }
                else
                {
                    Console.WriteLine("无效的命令。请输入/help查看可用命令。");
                }

            }
        }

        public void Log(string message)
        {
            try
            {
                if (message.Trim() != "")
                {
                    lock (_cacheLock)
                    {
                        foreach (var line in message.Split('\n'))
                        {
                            _logCache.Add($"{DateTime.Now}: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in cache log file: {ex.Message}");
                Log($"Error in cache log file: {ex.Message}");
            }
        }
        private readonly object _fileLock = new object();

        private void FlushCache(object state)
        {
            List<string> logsToWrite;

            lock (_cacheLock)
            {
                logsToWrite = new List<string>(_logCache);
                _logCache.Clear();
            }

            if (logsToWrite.Count > 0)
            {
                try
                {
                    // 写入主日志文件
                    lock (_fileLock)
                    {
                        using (StreamWriter logFile = new StreamWriter(logFilePath, true))
                        {
                            foreach (var log in logsToWrite)
                            {
                                logFile.WriteLine(log);
                            }
                        }
                    }

                    // 写入缓存文件
                    lock (_fileLock)
                    {
                        using (StreamWriter cacheFile = new StreamWriter(scacheFilePath, true))
                        {
                            foreach (var log in logsToWrite)
                            {
                                cacheFile.WriteLine(log);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
        }

        public void SearchLog(string searchKeyword)
        {
            try
            {
                var logContent = File.ReadAllLines(logFilePath);
                var matchingResults = logContent.Where(line => line.Contains(searchKeyword)).ToList();

                using (StreamWriter searchResultsFile = new StreamWriter(searchFilePath, false))
                {
                    foreach (var matchingLine in matchingResults)
                    {
                        searchResultsFile.WriteLine(matchingLine);
                    }
                }

                Console.WriteLine($"找到了 {matchingResults.Count} 条关于 \"{searchKeyword}\" 的记录日志, 我已保存(\"{searchFilePath}\"), 感觉良好.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在搜索日志时发生异常 {ex}");
                Log($"在搜索日志时发生异常 {ex}");
            }
        }

        // Mod : ClientInfo, Des.: 原版含有的模组,用于存储客户端信息的核心类, Server前置模组
        public class ClientInfo
        {
            public TcpClient TcpClient
            {
                get;
                set;
            }
            private string _connectionMessage;

            public string ConnectionMessage
            {
                get => _connectionMessage;
                set
                {
                    _connectionMessage = value;
                    Username = _connectionMessage.Split(':')[0];
                }
            }

            public string Username
            {
                get;
                set;
            }
        }
    }
}