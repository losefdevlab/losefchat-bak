using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Net;

namespace lcstd
{
    // Mod : Preset, Des.: LC原版预设工具模组
    // Part : 预设部分

    public partial class Preset
    {
        public int ipvx;
        public string ip;
        public int port;
        public string username;
        public string password;

        public string presetFilePath = "preset";
        public void ReadPreset(int preset) // 读取预设
        {
            if(!File.Exists(presetFilePath + preset.ToString() + ".txt"))
            {
                using (File.Create(presetFilePath + preset.ToString() + ".txt")) { }
                Console.WriteLine("您的预设文件"+ preset.ToString() +"第一次建立或者丢失了, 请填写预设文件信息,注意每一行都要小心填放,不能有行数缺失或者乱放");
                Console.WriteLine("预设置文件放在Losefchat主程序同目录下，名称为preset" + preset.ToString() + ".txt");
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
            string[] lines = File.ReadAllLines(presetFilePath + preset.ToString() + ".txt");
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
                    username = username.Replace(" ","");//删除空格
                }
                if (password.Contains(" "))
                {
                    password = password.Replace(" ","");//删除空格
                }
            }
        }
        
    }
}