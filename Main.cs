using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace V2ConsoleClient
{
    class Main
    {
        public static string ip = "18.192.64.12";
        //public static string ip = "127.0.0.1";
        public static int port = 8384;

        Connection connection;
        public Main()
        {
            connection = new Connection();

            connection.OnConnectedEvent += OnConnected;
            connection.OnDisconnectedEvent += OnDisconnected;
            connection.OnMessageReceivedEvent += OnMessageReceived;

            connection.Connect(ip, port);

            //Task consoleReaderTask = new Task(ReadConsole);
            //consoleReaderTask.Start();

            while (true) ReadConsole();
        }

        void ReadConsole()
        {
            string consoleString = Console.ReadLine();

            if (consoleString != "") connection.SendMessage(consoleString);
        }

        void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[SERVER_CONNECTED][{endPoint}]");
        }
        void OnDisconnected()
        {
            Console.WriteLine($"[SERVER_DISCONNECTED][{ip}]");
        }
        void OnMessageReceived(string message)
        {
            Console.WriteLine($"[SERVER_MESSAGE][{ip}]: {message}");
        }
    }
}
