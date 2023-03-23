using Crestron.SimplSharp;
using System.Collections.Generic;
using System.Linq;

namespace PepperDash.Core.Comm
{
    /// <summary>
    /// Background class that manages debug features for sockets
    /// </summary>
    public static class CommStatic
    {
        static List<ISocketStatus> Sockets = new List<ISocketStatus>();

        /// <summary>
        /// Sets up the backing class. Adds console commands for S#Pro programs
        /// </summary>
        static CommStatic()
        {
            if (CrestronEnvironment.RuntimeEnvironment == eRuntimeEnvironment.SimplSharpPro)
            {
                CrestronConsole.AddNewConsoleCommand(SocketCommand, "socket", "socket commands: list, send, connect, disco",
                    ConsoleAccessLevelEnum.AccessOperator);
            }
        }

        static void SocketCommand(string s)
        {
            //          0            1        2       
            //socket command number/key/all param
            //socket list
            //socket send 4 ver -v\n

            if (string.IsNullOrEmpty(s))
                return;
            var tokens = s.Split(' ');
            if (tokens.Length == 0)
                return;
            var command = tokens[0].ToLower();
            if (command == "connect")
            {

            }
            else if (command == "disco")
            {

            }
            else if (command == "list")
            {
                CrestronConsole.ConsoleCommandResponse("{0} sockets", Sockets.Count);
                if (Sockets.Count == 0)
                    return;
                // get the longest key name, for formatting
                var longestLength = Sockets.Aggregate("",
                    (max, cur) => max.Length > cur.Key.Length ? max : cur.Key).Length;
                for (int i = 0; i < Sockets.Count; i++)
                {
                    var sock = Sockets[i];
                    CrestronConsole.ConsoleCommandResponse("{0} {1} {2} {3}",
                        i, sock.Key, GetSocketType(sock), sock.ClientStatus);
                }
            }
            else if (command == "send")
            {

            }
        }

        /// <summary>
        /// Helper for socket list, to show types
        /// </summary>
        static string GetSocketType(ISocketStatus sock)
        {
            if (sock is GenericSshClient)
                return "SSH";
            else if (sock is GenericTcpIpClient)
                return "TCP-IP";
            else
                return "?";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        public static void AddSocket(ISocketStatus socket)
        {
            if (!Sockets.Contains(socket))
                Sockets.Add(socket);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        public static void RemoveSocket(ISocketStatus socket)
        {
            if (Sockets.Contains(socket))
                Sockets.Remove(socket);
        }
    }
}