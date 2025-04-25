using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using lcstd;
using System.Runtime.InteropServices;

// Mod : 程序, Des.: LC主程序类模组,包含模式选择、启动、模组加载基本重要功能
class 程序
{
    static void Main(string[] args)
    {
        StreamWriter inputconsole = new StreamWriter("clientinput");
        if (args.Length > 0 && args[0] == "-ci")
        {

        }
        Console.WriteLine("欢迎使用LosefChat v2.0.r1.b24(输出模式启动,输入模式请-ci或者-si)\n输入1 开始聊天,输入2 server,输入3 EXIT");
        while (true)
        {
            if (!int.TryParse(Console.ReadLine(), out int choose))
            {
                Console.WriteLine("无效输入，请输入1、2或3。");
                return;
            }

            if (choose == 1)
            {
                Client client = new Client();

                Preset preset = new Preset();
                preset.ReadPreset();

                client.Connect(preset.ipvx, preset.ip, preset.port, preset.username, preset.password);
            }
            else if (choose == 2)
            {
                Console.Write("接下来端口号\n如果您不输入,则默认选择2,\n如果您不是Windows系统,则默认选择9002\n建议不是Windows系统的选择"+
                    "9002,原因为，因为0-1024会被某些操作系统管控"
                    +"\n请输入端口号: ");
                if (!int.TryParse(Console.ReadLine(), out int 端口))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        端口 = 2;
                    }
                    else
                    {
                        端口 = 9002;
                    }
                }

                Server server = new Server(端口);

                server.Start();
            }
            else if (choose == 3)
            {
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("无效输入，请输入1、2或3。");
            }

        }

    }
}