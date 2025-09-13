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

namespace losefchat.lcstd.server
{
    public partial class Server
    {
        /// <summary>
        /// 存储被锁定用户的用户名与解锁时间的字典
        /// </summary>
        private Dictionary<string, DateTime> lockedUsers = new Dictionary<string, DateTime>();

        /// <summary>
        /// 存储用户登录尝试次数的字典
        /// </summary>
        private Dictionary<string, int> loginAttempts = new Dictionary<string, int>();

        /// <summary>
        /// 存储用户最后登录尝试时间的字典
        /// </summary>
        private Dictionary<string, DateTime> lastAttemptTime = new Dictionary<string, DateTime>();

        /// <summary>
        /// 定时重置登录尝试次数的方法
        /// </summary>
        /// <param name="state">定时器状态对象(未使用)</param>
        private void ResetLoginAttempts(object? state)
        {
            lock (lockObject)
            {
                loginAttempts.Clear();
                lastAttemptTime.Clear();
            }
        }
    }
}