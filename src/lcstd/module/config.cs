namespace losefchat.lcstd.module
{
    
    /// <summary>
    /// LC原版内含服务端配置工具
    /// </summary>
    public partial class Config
    {
        /// <summary>
        /// 获取或设置服务器端口号
        /// </summary>
        public int port;
        /// <summary>
        /// 获取或设置服务器名称
        /// </summary>
        public string sn;
        /// <summary>
        /// 获取或设置服务器名称
        /// </summary>
        public string sd;

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string configFilePath = "config";

        /// <summary>
        /// 从服务器配置文件中读取服务器配置并进行有效性验证
        /// </summary>
        public void ReadConfig()
        {
            while (true)
            {
                if (!File.Exists(configFilePath + ".txt"))
                {
                    using (File.Create(configFilePath + ".txt")) { }
                    Console.WriteLine("您的服务器配置文件" + "第一次建立或者丢失了或者需要重新编写, 请填写预设文件信息,注意每一行都要小心填放,不能有行数缺失或者乱放");
                    Console.WriteLine("预设置文件放在Losefchat主程序同目录下，名称为config.txt");
                    Console.WriteLine("请按照以下格式填写:");
                    Console.WriteLine("第一行:您的所用的端口号,如果您在Linux系的系统上使用0-1024端口号失败,那是因为端口管控机制,建议使用9002");
                    Console.WriteLine("第二行:服务器名称");
                    Console.WriteLine("第三行:服务器描述");
                    Console.WriteLine("填写完之后,按任意键继续...");
                    Console.ReadLine();

                }
                string[] lines = File.ReadAllLines(configFilePath + ".txt");
                if (lines.Length >= 2)
                {

                    port = int.Parse(lines[0]);
                    sn = lines[1];
                    sd = lines[2];
                    if (port <= 0 || port > 65535)
                    {
                        Console.WriteLine("[端口]您确定您这个端口是现代网络协议所规定的？请重新填写");
                        continue;
                    }

                    if (sn.Trim() == "")
                    {
                        Console.WriteLine("[服务器名称]咱这不支持无名氏");
                    }

                    break;
                }
            }
        }
    }
}