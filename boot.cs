using LosefDevLab.LosefChat.lcstd;
using System.Runtime.InteropServices;

// Mod : Boot, Des.: LosefChat 启动器
class Boot
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
                Console.WriteLine(@"  _                             __    ____   _               _");
                Console.WriteLine(@" | |       ___    ___    ___   / _|  / ___| | |__     __ _  | |_");
                Console.WriteLine(@" | |      / _ \  / __|  / _ \ | |_  | |     | '_ \   / _` | | __|");
                Console.WriteLine(@" | |___  | (_) | \__ \ |  __/ |  _| | |___  | | | | | (_| | | |_");
                Console.WriteLine(@" |_____|  \___/  |___/  \___| |_|    \____| |_| |_|  \__,_|  \__|");
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
                    using (StreamWriter sw = new StreamWriter(inputFilePath, true))
                    {
                        sw.WriteLine(cinp);
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
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
            Console.WriteLine("欢迎使用LosefChat v3.0.r3.b66\n客户端请注意:正常启动后，仅输出，输入模式请另启动程序（使用-ci附加参数启动程序）\n输入1 开始聊天,输入2 server,输入3 EXIT,输入4发行说明");
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
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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
                else if (choose == 4)
                {
                    Console.WriteLine(@"Losefchat 3.0.r3.b66 发行说明：");
                    Console.WriteLine(@"-------------------------------------");
                    Console.WriteLine(@"现在更方便于开发者开发，因为我们添加了IDE智能信息提示");
                    Console.WriteLine(@"并且我们添加了发行说明，现在您完全不必去github查看发行说明");
                    Console.WriteLine(@"-------------------------------------");
                    Console.WriteLine(@"GitHub：https://github.com/losefdevlab/losefchat");
                    Console.WriteLine(@"Email:along-losef@outlook.com");
                    
                }
                else
                {
                    Console.WriteLine("无效输入，请输入1、2或3。");
                }
            }
        }
    }
}