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
        string inputFilePath = ".ci";
        if (!File.Exists(inputFilePath))
        {
            using (File.Create(inputFilePath)) { }
        }

        if (args.Length > 0 && args[0] == "-ci")
        {
            while (true)
            {
                Console.Write("> ");
                string? cinp = Console.ReadLine();
                if (cinp == "exit")
                {
                    Environment.Exit(0);
                }
                else if (cinp?.Length > 1000)
                {
                    Console.Clear();
                    Console.Write("输入过长，请重新输入。");
                    Thread.Sleep(1000);
                    continue;
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(inputFilePath, true))
                    {
                        sw.WriteLine(cinp);
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("欢迎使用LosefChat v3.0.r2.b31\n客户端请注意:正常启动后，仅输出，输入模式请另启动程序（使用-ci附加参数启动程序）\n输入1 开始聊天,输入2 server,输入3 EXIT");
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
                    Console.Write("接下来端口号\n如果您不输入,则默认选择2,\n如果您不是Windows系统,则默认选择9002\n建议不是Windows系统的选择" +
                        "9002,原因为，因为0-1024会被某些操作系统管控"
                        + "\n请输入端口号: ");
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
}