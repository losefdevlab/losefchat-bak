using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Net;

namespace LosefDevLab.LosefChat.lcstd
{
/// <summary>
/// LC原版内含客户端预设工具
/// </summary>
    public partial class Preset
    {
        /// <summary>
        /// 获取或设置IP协议版本(4或6)
        /// </summary>
        public int ipvx;
        /// <summary>
        /// 获取或设置服务器IP地址
        /// </summary>
        public string ip;
        /// <summary>
        /// 获取或设置服务器端口号
        /// </summary>
        public int port;
        /// <summary>
        /// 获取或设置用户名
        /// </summary>
        public string username;
        /// <summary>
        /// 获取或设置用户密码
        /// </summary>
        public string password;

        /// <summary>
        /// 预设文件路径
        /// </summary>
        public string presetFilePath = "preset";

        /// <summary>
        /// 从预设文件中读取连接配置并进行有效性验证
        /// </summary>
        public void ReadPreset() // 读取预设
        {
            while (true)
            {
                if (!File.Exists(presetFilePath + ".txt"))
                {
                    using (File.Create(presetFilePath + ".txt")) { }
                    Console.WriteLine("您的预设文件" + "第一次建立或者丢失了或者需要重新编写, 请填写预设文件信息,注意每一行都要小心填放,不能有行数缺失或者乱放");
                    Console.WriteLine("预设置文件放在Losefchat主程序同目录下，名称为preset.txt");
                    Console.WriteLine("请按照以下格式填写:");
                    Console.WriteLine("第一行:您的IP协议(4/6)");
                    Console.WriteLine("第二行:服务器IP地址");
                    Console.WriteLine("第三行:端口号");
                    Console.WriteLine("第四行:用户名(空格会被忽略,下一项同,为空就使用计算机名称)");
                    Console.WriteLine("第五行:密码");
                    Console.WriteLine("填写完之后,按任意键继续...");
                    Console.ReadLine();

                }
                //读取每一行:第一行放IP协议选择,第二行放IP地址,第三行放端口号,第四行放用户名,第四行如果为空就使用计算机名称,第五行放密码
                string[] lines = File.ReadAllLines(presetFilePath + ".txt");
                if (lines.Length >= 4)
                {
                    ipvx = int.Parse(lines[0]);
                    ip = lines[1];
                    port = int.Parse(lines[2]);
                    username = lines[3];
                    password = lines[4];

                    if (string.IsNullOrEmpty(username))
                    {
                        username = Environment.MachineName;
                    }
                    if (username.Contains(" "))
                    {
                        username = username.Replace(" ", "");//删除空格
                    }
                    if (password.Contains(" "))
                    {
                        password = password.Replace(" ", "");//删除空格
                    }
                    if (ipvx != 4 && ipvx != 6)
                    {
                        Console.WriteLine("您设置的IP协议来自于异世界而不来自于这里,请重新填写(填写4/6)");
                        continue;
                    }
                    if (string.IsNullOrEmpty(ip))
                    {
                        Console.WriteLine("IP为空,疑似用户想连接/dev/null,请重新填写");
                        continue;
                    }
                    if (port <= 0 || port > 65535)
                    {
                        Console.WriteLine("您确定您这个端口是现代网络协议所规定的？请重新填写");
                        continue;
                    }

                    break;
                }
            }
        }
    }
}