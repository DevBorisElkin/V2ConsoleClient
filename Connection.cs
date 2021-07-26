using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace V2ConsoleClient
{

    class Connection
    {
        public delegate void OnConnectedDelegate(EndPoint endPoint);
        public event OnConnectedDelegate OnConnectedEvent;

        public delegate void OnDisconnectedDelegate();
        public event OnDisconnectedDelegate OnDisconnectedEvent;

        public delegate void OnMessageReceivedDelegate(string message, MessageProtocol protocol);
        public event OnMessageReceivedDelegate OnMessageReceivedEvent;

        #region variables
        public Socket socket;
        public bool connected;

        public enum MessageProtocol { TCP, UDP}

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
                socket.BeginConnect(remoteEP, new AsyncCallback(ConnectedCallback), socket);
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
        void ConnectedCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);
                OnConnected(client.RemoteEndPoint);
                connected = true;

                Task listenToIncomingMessages = new Task(ReceiveMessages);
                listenToIncomingMessages.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[CONNECTION_ERROR]: connection attempt failed - timed out");
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
            connected = false;
            OnDisconnected();
        }

        // [SEND MESSAGE TO SERVER]
        public void SendMessage(string message, MessageProtocol mp = MessageProtocol.TCP)
        {
            if(mp.Equals(MessageProtocol.TCP))
                if (connected)
                {
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    socket.Send(data);
                    return;
                }

            if (mp.Equals(MessageProtocol.UDP))
            {
                UDP.SendMessageUdp(message);
            }
        }
        public void SendMessage(byte[] message)
        {
            if (connected)
                socket.Send(message);
        }
        // [CALLBACKS]
        void OnConnected(EndPoint endPoint){ OnConnectedEvent?.Invoke(endPoint); }
        void OnDisconnected(){ OnDisconnectedEvent?.Invoke(); }
        public void OnMessageReceived(string message, MessageProtocol mp = MessageProtocol.TCP){ OnMessageReceivedEvent?.Invoke(message, mp); }
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
    static class UDP
    {
        static IPEndPoint remoteEpUdp;
        static Socket udpSocket;
        static Connection connection;

        static string address;
        static int portUdp;

        public static void ConnectTo(string _address, int _port, Connection _connection)
        {
            address = _address;
            portUdp = _port;
            connection = _connection;
            try
            {
                remoteEpUdp = new IPEndPoint(IPAddress.Any, portUdp);
                udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udpSocket.Bind(remoteEpUdp);

                Task taskListenUDP = new Task(ListenUDP);
                taskListenUDP.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }
        public static void SendMessageUdp(string message)
        {
            EndPoint remotePoint = new IPEndPoint(IPAddress.Parse(address), portUdp);
            byte[] data = Encoding.Unicode.GetBytes(message);
            udpSocket.SendTo(data, remotePoint);
        }
        static StringBuilder builder;
        public static bool listening;
        private static void ListenUDP()
        {
            try
            {
                Thread.Sleep(1000);
                SendMessageUdp("init_udp");

                listening = true;

                byte[] data = new byte[1024];
                EndPoint remoteIp = new IPEndPoint(IPAddress.Any, portUdp);

                int bytes;
                while (listening)
                {
                    builder = new StringBuilder();
                    do
                    {
                        bytes = udpSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (udpSocket.Available > 0);

                    connection.OnMessageReceived(builder.ToString(), Connection.MessageProtocol.UDP);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " ||| " + ex.StackTrace);
            }
            finally
            {
                CloseUdp();
            }
        }

        private static void DelayedInitCall()
        {
            Thread.Sleep(1000);
            SendMessageUdp("init_udp");
        }
        private static void CloseUdp()
        {
            Console.WriteLine("[SYSTEM_MESSAGE]: closed udp");
            listening = false;
            if (udpSocket != null)
            {
                udpSocket.Shutdown(SocketShutdown.Both);
                udpSocket.Close();
                udpSocket = null;
            }
        }
    }
}
