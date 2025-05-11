using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using LosefDevLab.LosefChat.lcstd;
using System.Runtime.InteropServices;

// Mod : Boot, Des.: LosefChat 启动器
class Boot
{
    static void Main(string[] args)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string inputFilePath = Path.Combine(baseDirectory, ".ci");

        if (args.Length > 0 && args[0] == "-ci")
        {
            while (true)
            {
                Console.WriteLine(@"  _                             __    ____   _               _");
                Console.WriteLine(@" | |       ___    ___    ___   / _|  / ___| | |__     __ _  | |_");
                Console.WriteLine(@" | |      / _ \  / __|  / _ \ | |_  | |     | '_ \   / _` | | __|");
                Console.WriteLine(@" | |___  | (_) | \__ \ |  __/ |  _| | |___  | | | | | (_| | | |_");
                Console.WriteLine(@" |_____|  \___/  |___/  \___| |_|    \____| |_| |_|  \__,_|  \__|");
                Console.WriteLine();
                Console.WriteLine(@"  _              _     _            ____   _               _        ____   _               _     _");
                Console.WriteLine(@" | |       ___  | |_  ( )  ___     / ___| | |__     __ _  | |_     / ___| | |__     __ _  | |_  | |");
                Console.WriteLine(@" | |      / _ \ | __| |/  / __|   | |     | '_ \   / _` | | __|   | |     | '_ \   / _` | | __| | |");
                Console.WriteLine(@" | |___  |  __/ | |_      \__ \   | |___  | | | | | (_| | | |_    | |___  | | | | | (_| | | |_  |_|");
                Console.WriteLine(@" |_____|  \___|  \__|     |___/    \____| |_| |_|  \__,_|  \__|    \____| |_| |_|  \__,_|  \__| (_)");
                Console.WriteLine("------------------------------------------------------------------------------------------------------");
                Console.WriteLine("LosefChat Client 纯输入模式");
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
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(inputFilePath, true))
                        {
                            sw.WriteLine(cinp);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"写入.ci文件时发生异常: {ex.Message}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine(@"  _                             __    ____   _               _");
            Console.WriteLine(@" | |       ___    ___    ___   / _|  / ___| | |__     __ _  | |_");
            Console.WriteLine(@" | |      / _ \  / __|  / _ \ | |_  | |     | '_ \   / _` | | __|");
            Console.WriteLine(@" | |___  | (_) | \__ \ |  __/ |  _| | |___  | | | | | (_| | | |_");
            Console.WriteLine(@" |_____|  \___/  |___/  \___| |_|    \____| |_| |_|  \__,_|  \__|");
            Console.WriteLine();
            Console.WriteLine(@"  _              _     _            ____   _               _        ____   _               _     _");
            Console.WriteLine(@" | |       ___  | |_  ( )  ___     / ___| | |__     __ _  | |_     / ___| | |__     __ _  | |_  | |");
            Console.WriteLine(@" | |      / _ \ | __| |/  / __|   | |     | '_ \   / _` | | __|   | |     | '_ \   / _` | | __| | |");
            Console.WriteLine(@" | |___  |  __/ | |_      \__ \   | |___  | | | | | (_| | | |_    | |___  | | | | | (_| | | |_  |_|");
            Console.WriteLine(@" |_____|  \___|  \__|     |___/    \____| |_| |_|  \__,_|  \__|    \____| |_| |_|  \__,_|  \__| (_)");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("欢迎使用LosefChat v3.0.r3.b54\n客户端请注意:正常启动后，仅输出，输入模式请另启动程序（使用-ci附加参数启动程序）\n输入1 开始聊天,输入2 server,输入3 EXIT");
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out int choose))
                {
                    Console.WriteLine("无效输入，请输入1、2或3。");
                    continue;
                }

                if (choose == 1)
                {
                    try
                    {
                        Client client = new Client();

                        Preset preset = new Preset();
                        preset.ReadPreset();

                        client.Connect(preset.ipvx, preset.ip, preset.port, preset.username, preset.password);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"启动客户端时发生异常: {ex.Message}");
                    }
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
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            端口 = 9002;
                        }
                    }

                    try
                    {
                        Server server = new Server(端口);
                        server.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"启动服务器时发生异常: {ex.Message}");
                    }
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