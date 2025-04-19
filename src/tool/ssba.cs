using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Net;
//社区对bot官方开发的要求催的太紧了怎么办？周末更新呗你还有什么办法！

namespace lcstd
{
    public partial class Server
    {
        // Part : LosefChat Server Side Bot API(lssba), Des.: 给开发者提供的API形式的服务端BOT开发语言
        // Version : lssba standard 1
        public string[] code;// = new string{
        // "name bot",
        // "description 测试机器人",
        // "log 测试机器人启动成功",
        // "cmddef test",
        // "cmdlink test test",
        // "cmdrun test",
        // "cmd",
        // "cmdstop",
        // "cmdsetp test private"
        // };
        private void lssba(string[] codei,Dictionary<string, string> cmdlink_dic)
        {
            string[] botcode = new string[2048];

            botcode[0] = "name";//命名机器人的关键字 后面添加空格跟着机器人名
            //必须要在代码第一行命名,否则无法执行任何操作

            botcode[1] = "description";//描述机器人的关键字 后面添加空格跟着机器人描述
            //可不必

            botcode[2] = "log";//记录日志的关键字 后面添加空格跟着要记录的日志内容

            botcode[3] = "broadcast";//广播消息的关键字,后面添加空格跟着要广播的文本

            botcode[4] = "cmddef";//创建命令关键字 后面添加空格跟着命令名(命令名不能有空格,有空格直接删除)
            //默认创建的命令的权限都是public

            botcode[5] = "cmdlink";//指定最新创建的命令实现方式(连接一个c#方法,方法必须在Server class) 后面添加空格跟着方法名
            //可以不连接方法,直接使用命令,但效果就是没有效果 还有不要连接有参数的方法,至少在这个lssba标准1中不能连接,否则你会后悔的,未来的新标准可能更新连接参数

            botcode[6] = "cmdrun";//直接运行命令，而非让用户调用这条命令 后面添加空格跟着命令名

            botcode[7] = "cmd";//开启命令

            botcode[8] = "cmdstop";//关闭命令

            botcode[9] = "cmdsetp";//设置命令为私有的或公有的 后面添加空格跟着命令在空格跟着public或private

            bpf_count++;
            //检查第一行是否为name xxxxx类似的格式
            if ((codei[0].Substring(0, 4) == "name"))
            {
                bpf[bpf_count].name = codei[0].Substring(5);
                for (int i = 1; i < codei.Length; i++)
                {
                    if (codei[i].Substring(0, 11) == "description")
                    {
                        bpf[bpf_count].description = codei[i].Substring(12);
                    }
                    else if (codei[i].Substring(0, 3) == "log")
                    {
                        Log(codei[i].Substring(4));
                    }
                    else if (codei[i].Substring(0, 9) == "broadcast")
                    {
                        BroadcastMessage(codei[i].Substring(10));
                    }
                }
            }
            else
            {
                Log("lssba ERROR: LSSBA标准一规定, 在代码第一行必须name!");
                Log("lssba ERROR: 把第一行改成name xxxx");
                
            }

        }
        public Botprofile[] bpf;
        public int bpf_count = -1;
        public class Botprofile
        {
            public string name;
            public string description;
            public string[] mename;//method name
            public Dictionary<string, string> cmdlink_dic;//method link dictionary
        }
    }
}