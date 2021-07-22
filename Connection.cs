using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace V2ConsoleClient
{
    class Connection
    {
        public delegate void OnConnectedDelegate();
        public event OnConnectedDelegate OnConnectedEvent;

        public delegate void OnDisconnectedDelegate();
        public event OnDisconnectedDelegate OnDisconnectedEvent;

        public delegate void OnMessageReceivedDelegate(string message);
        public event OnMessageReceivedDelegate OnMessageReceivedEvent;

        #region variables
        public Socket socket;
        public bool connected;

        string ip;
        int port;
        #endregion
        
        // [CONNECT TO SERVER AND START RECEIVING MESSAGES]
        public void Connect(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(remoteEP);
                connected = true;

                OnConnected();

                //string serverConnectedTo = socket.RemoteEndPoint.ToString();
                //Console.WriteLine($"[SERVER_CONNECT][{serverConnectedTo}]");

                Task listenToIncomingMessages = new Task(ReceiveMessages);
                listenToIncomingMessages.Start();
            }
            catch (SocketException se)
            {
                Disconnect();
            }
            catch (Exception e)
            {
                Disconnect();
            }
        }
        // [RECEIVE MESSAGES FROM SERVER]
        void ReceiveMessages()
        {
            while (connected)
            {
                    try
                    {
                        OnMessageReceived(ConnectionUtil.ReadLine(socket));
                    }
                    catch (Exception e)
                    {
                        Disconnect();
                        break;
                    }
            }
        }
        // [DISCONNECT FROM THE SERVER]
        public void Disconnect()
        {
            ConnectionUtil.Disconnect(socket);
            //Console.WriteLine($"[SERVER_DISCONNECT][{ip}]");
            connected = false;
            OnDisconnected();
        }

        // [SEND MESSAGE TO SERVER]
        public void SendMessage(string message)
        {
            if (connected)
            {
                byte[] data = Encoding.Unicode.GetBytes(message);
                socket.Send(data);
            }
        }
        public void SendMessage(byte[] message)
        {
            if (connected)
                socket.Send(message);
        }
        // [CALLBACKS]
        public void OnConnected(){ OnConnectedEvent?.Invoke(); }
        public void OnDisconnected(){ OnDisconnectedEvent?.Invoke(); }
        public void OnMessageReceived(string message){ OnMessageReceivedEvent?.Invoke(message); }
    }
    public static class ConnectionUtil
    {
        // #Listen and return message
        static byte[] bytesArray;
        public static string ReadLine(Socket socket)
        {
            bytesArray = new byte[1024];
            StringBuilder builder = new StringBuilder();
            int bytes = 0;

            do
            {
                bytes = socket.Receive(bytesArray, bytesArray.Length, 0);
                builder.Append(Encoding.Unicode.GetString(bytesArray, 0, bytes));
            }
            while (socket.Available > 0);

            return builder.ToString();
        }

        public static void Disconnect(Socket socket)
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }
}
