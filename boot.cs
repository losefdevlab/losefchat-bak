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
    static void Main()
    {
        Console.WriteLine("欢迎使用LosefChat v2.0.r1.b12\n输入1 开始聊天,输入2 server,输入3 EXIT");
        while (true)
        {
            if (!int.TryParse(Console.ReadLine(), out int choose))
            {
                Console.WriteLine("无效输入，请输入1、2或3。");
                return;
            }

            if (choose == 1)
            {
                Console.WriteLine("请选择预设文件:");
                if (!int.TryParse(Console.ReadLine(), out int PresetChoose))
                {
                    Console.WriteLine("无效输入，请输入一个数字");
                }
                Client client = new Client(PresetChoose);

                Preset preset = new Preset();
                preset.ReadPreset(PresetChoose);

                client.Connect(preset.ipvx, preset.ip, preset.port, preset.username, preset.password);
            }
            else if (choose == 2)
            {
                Console.Write("接下来端口号，如果您不输入,则默认选择2,如果您不是Windows系统,则默认选择9002\n请输入端口号: ");
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