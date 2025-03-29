using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

// Mod : Server, Des.: LC原版服务端核心类模组
// Part : Server主部分
public partial class Server
{
    public TcpListener tcpListener;
    public List<ClientInfo> clientList = new List<ClientInfo>();
    public object lockObject = new object();
    public string logFilePath = "log.txt"; // Log file path
    public string searchFilePath = "search_results.txt"; // Search results file path
    public string bannedUsersFilePath = "banned_users.txt"; // Banned users file path
    public string whiteListFilePath = "white_list.txt"; // White list file path
    public HashSet<string> bannedUsersSet;
    public HashSet<string> whiteListSet;
    public bool isServerUseTheWhiteList = false;

    public Server(int port)
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
        bannedUsersSet = File.ReadAllLines(bannedUsersFilePath).ToHashSet();
        if (!File.Exists(whiteListFilePath))
        {
            using (File.Create(whiteListFilePath)) { }
        }
        whiteListSet = File.ReadAllLines(whiteListFilePath).ToHashSet();
        Timer resetAttemptsTimer = new Timer(ResetLoginAttempts, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        tcpListener = new TcpListener(IPAddress.Any, port);
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
        stopwatch.Stop();
        TimeSpan elapsed = stopwatch.Elapsed;
        Log($"Server started. [{elapsed.TotalMilliseconds} s]");

        tcpListener.Start();

        Thread consoleInputThread = new Thread(new ThreadStart(ReadConsoleInput));
        consoleInputThread.Start();

        while (true)
        {
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
        ClientInfo clientInfo = (ClientInfo)clientInfoObj;
        TcpClient tcpClient = clientInfo.TcpClient;
        //封禁用户检测机制
        if (bannedUsersSet.Contains(clientInfo.Username)) 
        {
            
            Log($"用户 '{clientInfo.Username}' 被封禁, 无法连接.");
            SendMessage(clientInfo, $"你 '{clientInfo.Username}' 被封禁, 无法连接.");
            Thread.Sleep(500);
            tcpClient.Close();
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
            catch
            {
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

        Console.WriteLine("用户 '" + clientInfo.Username + "' 下线了.");
        lock (lockObject)
        {
            clientList.Remove(clientInfo);
        }
        BroadcastMessage($"{clientInfo.Username} 下线.");
        tcpClient.Close();
    }

    public void BroadcastMessage(string message, string senderUsername = "")
    {
        byte[] broadcastBytes = Encoding.UTF8.GetBytes(message);

        lock (lockObject)
        {
            foreach (var client in clientList)
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
        }

        // 记录广播消息到日志文件
        Log(message);
    }

    public void SendMessage(ClientInfo clientInfo, string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        clientInfo.TcpClient.GetStream().Write(messageBytes, 0, messageBytes.Length);
        clientInfo.TcpClient.GetStream().Flush();
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
                    Console.WriteLine("    "+client.Username);//在这里遍历然后显示
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
                        Log(client.Username+" 被添加到了白名单,可以加入服务器了!");
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"在允许白名单用户加入时发生异常 {ex}");
            Log($"在允许白名单用户加入时发生异常 {ex}");
        }
    }

    public void ReadConsoleInput()
    {
        while (true)
        {
            string input = Console.ReadLine();

            if (input.StartsWith("/kick"))
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
        }
    }

    public void Log(string message)
    {
        using (StreamWriter logFile = new StreamWriter(logFilePath, true))
        {
            logFile.WriteLine($"{DateTime.Now}: {message}");
            Console.WriteLine($"\a{DateTime.Now}: {message}");
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
        public TcpClient TcpClient {
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

        public string Username {
            get;
            set;
        }
    }

    //Mod开发区域
    //以下区域供Mod的开发
    //Mod开发规则:
    //一个mod只能使用一个Class,Class名称必须为mod名称
    // Mod必须要有一个构造函数,构造函数必须要有一个Client/Server形参,并且在模组类内部创建一个和形参同等类型的对象
    // 使这个内部对象和实参(形参)完全一样
    // 必须为public类

    // Mod : mod, Des.: 简易模组示例
    public class mod
    {
        public Server servercpy;
        public mod(Server server) {
            Thread a = new Thread(() => {
                while(true) servercpy = server;
            });
            a.Start();
        }
        public string Name {
            get;
            set;
        }
        public string Description {
            get;
            set;
        }
        public void Start()
        {
            // Console.WriteLine();
        }
    }
}