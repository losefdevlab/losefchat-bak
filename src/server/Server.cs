using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace LosefDevLab.LosefChat.lcstd
{
    // Mod : Server, Des.: LC原版服务端核心类模组
    // Part : Server主部分
    public partial class Server
    {
        public TcpListener? tcpListener;
        public List<ClientInfo> clientList = new List<ClientInfo>();
        public object lockObject = new object();
        public string logFilePath = $"log{DateTime.Now:yyyy}{DateTime.Now:MM}{DateTime.Now:dd}.txt"; // Log file path
        public string searchFilePath = "search_results.txt"; // Search results file path
        public string bannedUsersFilePath = "banned_users.txt"; // Banned users file path
        public string whiteListFilePath = "white_list.txt"; // White list file path
        public string scacheFilePath = "outputcache.txt";
        public HashSet<string> bannedUsersSet;
        public HashSet<string> whiteListSet;

        private readonly object _cacheLock = new object();
        private readonly List<string> _logCache = new List<string>();
        private readonly Timer? _flushTimer;
        private int _messageTokens = 7200; // 每小时最大消息数(m = message tokens, 7.2km/s)
        private readonly object _tokenLock = new object();
        private Timer? _tokenRefillTimer;

        public Server(int port)
        {
            Log($"Server port was set to {port}.");
            File.WriteAllText(scacheFilePath, string.Empty);
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
            bannedUsersSet = File.ReadAllLines(bannedUsersFilePath).ToHashSet();
            try
            {
                if (!File.Exists(whiteListFilePath))
                {
                    using (File.Create(whiteListFilePath)) { }
                }
                whiteListSet = File.ReadAllLines(whiteListFilePath).ToHashSet();
            }
            catch (Exception ex)
            {
                Log($"[WhiteList] 初始化白名单文件时发生异常: {ex.Message}");
                whiteListSet = new HashSet<string>();
            }
            Timer resetAttemptsTimer = new Timer(ResetLoginAttempts, null, TimeSpan.Zero, TimeSpan.FromDays(1));
            tcpListener = new TcpListener(IPAddress.Any, port);
            _flushTimer = new Timer(FlushCache, null, TimeSpan.Zero, TimeSpan.FromSeconds(0.5));
            _tokenRefillTimer = new Timer(RefillTokens, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        public void Stop()
        {
            tcpListener?.Stop();
            Log("服务器核心已关闭.");
        }

        public void Start()
        {
            Log("Server loading...");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
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
            bannedUsersSet = File.ReadAllLines(bannedUsersFilePath).ToHashSet();

            // Create or read white list file
            if (!File.Exists(whiteListFilePath))
            {
                using (File.Create(whiteListFilePath)) { }
            }
            if (!File.Exists(userFilePath))
            {
                using (File.Create(userFilePath)) { }
            }
            if (!File.Exists(pwdFilePath))
            {
                using (File.Create(pwdFilePath)) { }
            }
            tcpListener.Start();
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Log($"Server started. [{elapsed.TotalMilliseconds} s]");

            Thread consoleInputThread = new Thread(new ThreadStart(ReadConsoleInput));
            consoleInputThread.Start();

            while (true)
            {
                if (!File.Exists(scacheFilePath))
                {
                    using (File.Create(scacheFilePath)) { }
                }

                TcpClient tcpClient = tcpListener.AcceptTcpClient();

                // 发送 Challenge
                string nonce = Guid.NewGuid().ToString("N").Substring(0, 16);
                byte[] challengeBytes = Encoding.UTF8.GetBytes($"[CHALLENGE]{nonce}");
                tcpClient.GetStream().Write(challengeBytes, 0, challengeBytes.Length);
                tcpClient.GetStream().Flush();
                byte[] responseBytes = new byte[1024];
                int bytesRead = tcpClient.GetStream().Read(responseBytes, 0, responseBytes.Length);
                string clientResponse = Encoding.UTF8.GetString(responseBytes, 0, bytesRead).Trim();

                string secretKey = "losefchat-client-secret-key";// 验证是否为非法客户端的私钥
                string expectedResponse = ComputeSHA256Hash(nonce + secretKey);

                if (clientResponse != expectedResponse)
                {
                    Log($"[Auth] 非法客户端{tcpClient.Client.RemoteEndPoint}尝试连接，拒绝访问。");
                    byte[] denyBytes = Encoding.UTF8.GetBytes("[AUTH:FAILED]");
                    tcpClient.GetStream().Write(denyBytes, 0, denyBytes.Length);
                    tcpClient.Close();
                    continue;
                }
                else
                {
                    byte[] allowBytes = Encoding.UTF8.GetBytes("[AUTH:OK]");
                    Log($"[Auth] 允许客户端{tcpClient.Client.RemoteEndPoint}连接.");
                    tcpClient.GetStream().Write(allowBytes, 0, allowBytes.Length);
                    tcpClient.GetStream().Flush();
                }

                // 接收用户名
                byte[] usernameBytes = new byte[32567];
                int usernameBytesRead = tcpClient.GetStream().Read(usernameBytes, 0, 32567);
                string username = Encoding.UTF8.GetString(usernameBytes, 0, usernameBytesRead).Trim();

                // 接收密码
                byte[] passwordBytes = new byte[32567];
                int passwordBytesRead = tcpClient.GetStream().Read(passwordBytes, 0, 32567);
                string password = Encoding.UTF8.GetString(passwordBytes, 0, passwordBytesRead).Trim();

                if (!IsUserValid(username, password))
                {
                    Log($"拒绝了一个用户的连接请求: '{username}' 用户名或密码错误.");
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

                if (!whiteListSet.Contains(clientInfo.Username))
                {
                    Log($"用户 '{clientInfo.Username}' 不在白名单中, 无法连接.");
                    SendMessage(clientInfo, $"你 '{clientInfo.Username}' 不在白名单中, 无法连接.");
                    Thread.Sleep(500);
                    tcpClient.Close();
                    return;
                }

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
                        foreach (var client in clientList)
                        {
                            if (client.Username == senderUsername && mutedUsersSet.Contains(senderUsername))
                            {
                                continue;
                            }

                            NetworkStream clientStream = client.TcpClient.GetStream();
                            clientStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                            clientStream.Flush();
                        }
                    }
                    Log(message);
                }
                else
                {
                }
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
                        try
                        {
                            File.WriteAllLines(whiteListFilePath, whiteListSet);
                            Log($"[WhiteList] '{username}' 已添加到白名单.");
                        }
                        catch (IOException ioEx)
                        {
                            Log($"[WhiteList] 写入白名单文件时发生 I/O 错误: {ioEx.Message}");
                        }
                        catch (UnauthorizedAccessException authEx)
                        {
                            Log($"[WhiteList] 没有权限写入白名单文件: {authEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            Log($"[WhiteList] 写入白名单文件时发生未知错误: {ex.Message}");
                        }
                    }
                    else
                    {
                        Log($"[WhiteList] '{username}' 已经在白名单里面了,不要重复添加.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[WhiteList] 在添加到白名单时发生异常: {ex.Message}");
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
                        try
                        {
                            File.WriteAllLines(whiteListFilePath, whiteListSet);
                            Log($"[WhiteList] '{username}' 已从白名单中移除.");
                        }
                        catch (IOException ioEx)
                        {
                            Log($"[WhiteList] 写入白名单文件时发生 I/O 错误: {ioEx.Message}");
                        }
                        catch (UnauthorizedAccessException authEx)
                        {
                            Log($"[WhiteList] 没有权限写入白名单文件: {authEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            Log($"[WhiteList] 写入白名单文件时发生未知错误: {ex.Message}");
                        }
                    }
                    else
                    {
                        Log($"[WhiteList] '{username}' 本来就不在白名单里面,不要重复移除.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[WhiteList] 在从白名单中移除用户时发生异常: {ex.Message}");
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
                Log($"[WhiteList] 在允许白名单用户加入时发生异常 {ex}"); // 增加详细日志输出
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
                else if (input.Trim() == "/users")
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
                    string username = input.Substring(7);
                    AddToWhiteList(username);
                }
                else if (input.StartsWith("/rmwl"))
                {
                    string username = input.Substring(6);
                    RemoveFromWhiteList(username);
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
                    Console.WriteLine("/addwl <username>: 将用户添加到白名单");
                    Console.WriteLine("/rmwl <username>: 从白名单中移除用户");
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

        private void FlushCache(object? state)
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

        private void RefillTokens(object? state)
        {
            lock (_tokenLock)
            {

                _messageTokens = Math.Min(7200, _messageTokens + 2);
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