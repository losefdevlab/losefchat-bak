namespace LosefDevLab.LosefChat.lcstd
{
    // Mod : Server, Des.: LC原版服务端核心类模组
    // Part : 密码
    public partial class Server
    {
        /// <summary>
        /// 用户信息文件路径，默认为user.txt
        /// </summary>
        public string userFilePath = "user.txt";

        /// <summary>
        /// 密码存储文件路径，默认为pwd.txt
        /// </summary>
        public string pwdFilePath = "pwd.txt";

        /// <summary>
        /// 验证用户身份有效性（包含锁定检查/新用户创建/密码验证逻辑）
        /// </summary>
        /// <param name="username">待验证的用户名</param>
        /// <param name="password">待验证的密码</param>
        /// <returns>验证结果（true=通过，false=拒绝）</returns>
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
                string encryptedPassword = Convert.ToBase64String(System.Security.Cryptography.SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(password)));
                File.AppendAllText(pwdFilePath, encryptedPassword + Environment.NewLine);
                return true;
            }
            else
            {   // 旧用户逻辑
                string storedEncryptedPassword = passwords[index];
                string decryptedPassword = Convert.ToBase64String(System.Security.Cryptography.SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(password)));
                if (storedEncryptedPassword == decryptedPassword)
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

        /// <summary>
        /// 检查指定用户名是否可用（线程安全）
        /// </summary>
        /// <param name="username">待检查的用户名</param>
        /// <returns>可用性状态（true=可用，false=已被占用）</returns>
        public bool IsUsernameAvailable(string username)
        {
            lock (lockObject)
            {
                return !clientList.Exists(c => c.Username == username);
            }
        }
    }
}