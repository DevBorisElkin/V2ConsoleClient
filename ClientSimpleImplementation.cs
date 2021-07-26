using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static V2ConsoleClient.Connection;

namespace V2ConsoleClient
{
    // SIMPLE AND QUICK IMPLEMENTATION
    class ClientSimpleImplementation
    {
        public static string ip = "18.192.64.12";
        //public static string ip = "127.0.0.1";
        public static int portTcp = 8384;
        public static int portUdp = 8385;

        Connection connection;
        public ClientSimpleImplementation()
        {
            Console.Title = "Simple Console Client";
            connection = new Connection();
            connection.OnConnectedEvent += OnConnected;
            connection.OnDisconnectedEvent += OnDisconnected;
            connection.OnMessageReceivedEvent += OnMessageReceived;

            connection.Connect(ip, portTcp);
            UDP.ConnectTo(ip, portUdp, connection);

            while (true) ReadConsole();
        }

        void ReadConsole()
        {
            string consoleString = Console.ReadLine();

            if (consoleString != "")
            {
                if(consoleString.StartsWith("tcp "))
                {
                    consoleString = consoleString.Replace("tcp ", "");
                    connection.SendMessage(consoleString);
                }
                else if(consoleString.StartsWith("udp "))
                {
                    consoleString = consoleString.Replace("udp ", "");
                    connection.SendMessage(consoleString, MessageProtocol.UDP);
                }
                else
                {
                    connection.SendMessage(consoleString);
                }
            }
        }

        void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[SERVER_CONNECTED][{endPoint}]");
        }
        void OnDisconnected()
        {
            Console.WriteLine($"[SERVER_DISCONNECTED][{ip}]");
        }
        void OnMessageReceived(string message, MessageProtocol mp)
        {
            Console.WriteLine($"[SERVER_MESSAGE][{mp}][{ip}]: {message}");
        }
    }
}
