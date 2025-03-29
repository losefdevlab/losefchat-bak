using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

// Mod : 程序, Des.: LC主程序类模组,包含模式选择、启动、模组加载基本重要功能
class 程序
{
    static void Main()
    {
        Console.WriteLine("欢迎使用LosefChat v2.0.r1.b128\n输入1 开始聊天,输入2 服务器,输入3 EXIT");

        if (!int.TryParse(Console.ReadLine(), out int choose))
        {
            Console.WriteLine("无效输入，请输入1、2或3。");
            return;
        }

        if (choose == 1)
        {

            Client 客户端 = new Client();

            // 模组加载区域
            // 在这里加载模组
            // 下为简易示例,服务端模式相同操作
            Client.mod is_a_mod = new Client.mod(客户端);//<<< 必须要在参数表(括号)中加入它对应运行模式的对应类的已声明实例
            Thread modthread = new Thread(() =>
            {
                is_a_mod.Start();
            });
            modthread.Start();
            // 必须要使用一个线程来加载模组
            // 照这个例子，结合模组作者的文档在下面加载模组

            // 模组加载区域结束

            Console.WriteLine("请选择预设文件:");
            if (!int.TryParse(Console.ReadLine(), out int PresetChoose))
            {
                Console.WriteLine("无效输入，请输入一个数字");
            }

            Preset preset = new Preset();
            preset.ReadPreset(PresetChoose);

            客户端.Connect(preset.ipvx, preset.ip, preset.port , preset.username, preset.password);
        }
        else if (choose == 2)
        {
            Console.Write("请输入服务器端口号: ");
            if (!int.TryParse(Console.ReadLine(), out int 端口))
            {
                Console.WriteLine("无效的端口号。");
                return;
            }

            Server 服务器 = new Server(端口);

            // 模组加载区域
            // 在这里加载模组
            // 下为简易示例
            Server.mod is_a_mod = new Server.mod(服务器);//<<< 必须要在参数表(括号)中加入它对应运行模式的对应类的已声明实例
            Thread modthread = new Thread(() =>
            {
                is_a_mod.Start();
            });// 必须要使用一个线程来加载模组
            // 照这个例子，结合模组作者的文档在下面加载模组

            // 模组加载区域结束

            服务器.Start();
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