// Mod : Server, Des.: LC原版服务端核心类模組
// Part : Client Info

using System.Net.Sockets;

namespace LosefDevLab.LosefChat.lcstd
{
    public partial class Server
    {
        // Mod : ClientInfo, Des.: 原版含有的模組,用于存储客户端信息的核心类, Server前置模組
        public class ClientInfo
        {
            public TcpClient? TcpClient
            {
                get;
                set;
            }
            private string? _connectionMessage;

            public string? ConnectionMessage
            {
                get => _connectionMessage;
                set
                {
                    _connectionMessage = value;
                    Username = _connectionMessage?.Split(':')[0];
                }
            }

            public string? Username
            {
                get;
                set;
            }
        }
    }
}