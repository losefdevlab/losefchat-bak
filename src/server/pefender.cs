// Mod : Server, Des.: LC原版服务端核心类模组
// Part : Server Pefender 支持Tool
// Pefender为专有名词, 意为Password Defender, 即密码防御(防破解)
using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace lcstd
{
    public partial class Server
    {
        private Dictionary<string, DateTime> lockedUsers = new Dictionary<string, DateTime>();
        private Dictionary<string, int> loginAttempts = new Dictionary<string, int>();
        private Dictionary<string, DateTime> lastAttemptTime = new Dictionary<string, DateTime>();
        private void ResetLoginAttempts(object state) // 重置尝试次数
        {
            lock (lockObject)
            {
                loginAttempts.Clear();
                lastAttemptTime.Clear();
            }
        }
    }
}