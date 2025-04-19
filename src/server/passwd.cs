namespace lcstd
{
    // Mod : Server, Des.: LC原版服务端核心类模组
    // Part : 密码
    public partial class Server
    {
        public string userFilePath = "user.txt";
        public string pwdFilePath = "pwd.txt";
        private bool IsUserValid(string username, string password)
        {
            // 检查用户是否被锁定
            if (lockedUsers.ContainsKey(username))
            {
                if (DateTime.Now.Date == lockedUsers[username].Date)
                {
                    Log($"用户在之前'{username}' 因连续1次登录失败被锁定.");
                    return false;
                }
                else
                {
                    // 如果是新的一天，移除锁定状态
                    lockedUsers.Remove(username);
                }
            }

            var users = File.ReadAllLines(userFilePath);
            var passwords = File.ReadAllLines(pwdFilePath);

            int index = Array.IndexOf(users, username);

            if (index == -1)
            {   // 新用户逻辑
                File.AppendAllText(userFilePath, username + Environment.NewLine);
                File.AppendAllText(pwdFilePath, password + Environment.NewLine);
                return true;
            }
            else
            {   // 旧用户逻辑
                if (passwords[index] == password)
                {
                    // 登录成功，重置尝试次数
                    lock (lockObject)
                    {
                        if (loginAttempts.ContainsKey(username))
                        {
                            loginAttempts[username] = 0;
                        }
                    }
                    return true;
                }
                else
                {
                    // 登录失败，增加尝试次数
                    lock (lockObject)
                    {
                        if (!loginAttempts.ContainsKey(username))
                        {
                            loginAttempts[username] = 1;
                            lastAttemptTime[username] = DateTime.Now;
                        }
                        else
                        {
                            if (DateTime.Now.Date != lastAttemptTime[username].Date)
                            {
                                // 如果是新的一天，重置尝试次数
                                loginAttempts[username] = 0;
                                lastAttemptTime[username] = DateTime.Now;
                            }
                            else
                            {
                                loginAttempts[username]++;
                                if (loginAttempts[username] >= 1)
                                {
                                    Log($"用户 '{username}' 因连续1次登录失败被锁定.");
                                    lockedUsers[username] = DateTime.Now;
                                    return false;
                                }
                            }
                        }
                    }
                    return false;
                }
            }
        }
        public bool IsUsernameAvailable(string username)
        {
            lock (lockObject)
            {
                return !clientList.Exists(c => c.Username == username);
            }
        }
    }
}