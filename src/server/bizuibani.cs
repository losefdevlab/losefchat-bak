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
    // Mod : Server, Des.: LC原版服务端核心类模组
    // Part : 闭嘴吧你

    public partial class Server
    {
        /// <summary>
        /// 存储当前被禁言用户的用户名集合（线程安全）
        /// </summary>
        public HashSet<string> mutedUsersSet = new HashSet<string>();

        /// <summary>
        /// 将指定用户加入禁言列表并发送通知（线程安全）
        /// </summary>
        /// <param name="targetUsername">需要禁言的用户名</param>
        /// <exception cref="Exception">记录并捕获所有异常信息"></exception>
        public void MuteUser(string targetUsername)
        {
            try
            {
                lock (lockObject)
                {
                    if (!mutedUsersSet.Contains(targetUsername))
                    {
                        mutedUsersSet.Add(targetUsername);
                        Log($"用户 '{targetUsername}' 已被禁言.");
                        Console.WriteLine($"用户 '{targetUsername}' 已被禁言.");

                        // 通知该用户已被禁言
                        ClientInfo targetClient = clientList.FirstOrDefault(c => c.Username == targetUsername);
                        if (targetClient != null)
                        {
                            SendMessage(targetClient, "你已被管理员禁言！");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"用户 '{targetUsername}' 已经被禁言了，不要重复操作.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在禁言用户时发生异常: {ex}");
                Log($"在禁言用户时发生异常: {ex}");
            }
        }

        /// <summary>
        /// 从禁言列表中移除指定用户并发送通知（线程安全）
        /// </summary>
        /// <param name="targetUsername">需要解除禁言的用户名</param>
        /// <exception cref="Exception">记录并捕获所有异常信息"></exception>
        public void UnmuteUser(string targetUsername)
        {
            try
            {
                lock (lockObject)
                {
                    if (mutedUsersSet.Contains(targetUsername))
                    {
                        mutedUsersSet.Remove(targetUsername);
                        Log($"用户 '{targetUsername}' 已解除禁言.");
                        Console.WriteLine($"用户 '{targetUsername}' 已解除禁言.");

                        // 通知该用户已解除禁言
                        ClientInfo targetClient = clientList.FirstOrDefault(c => c.Username == targetUsername);
                        if (targetClient != null)
                        {
                            SendMessage(targetClient, "你已被管理员解除禁言！");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"用户 '{targetUsername}' 未被禁言，无法解除.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在解除禁言时发生异常: {ex}");
                Log($"在解除禁言时发生异常: {ex}");
            }
        }

    }
}