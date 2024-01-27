using System.Net.Sockets;

namespace NetworkServer {

    /// <summary>
    /// Connect to a TCP server.
    /// Fires the event OnMessageReceived.
    /// </summary>
    /// \todo Error handling when message has invalid format.
    /// <typeparam name="T">The type it handles.</typeparam>
    public class TCPClient<T>
    {

        private TcpClient tcpClient;
        private Thread clientThread;
        private NetworkStream clientStream;

        private string ip;
        private int port;

        public TCPClient(string _ip, int _port)
        {
            ip = _ip;
            port = _port;
        }

        public void Start()
        {
            tcpClient = new TcpClient();
            clientThread = new Thread(new ThreadStart(ConnectToServer));
            clientThread.Start();
        }

        private void ConnectToServer()
        {
            tcpClient.Connect(this.ip, this.port);
            clientStream = this.tcpClient.GetStream();

            byte[] serverResponseBuffer = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = this.clientStream.Read(serverResponseBuffer, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                Message<T> serverResponse = Message<T>.FromBytes(serverResponseBuffer);
                OnMessageReceived(serverResponse);

            }
        }

        public void Stop()
        {
            tcpClient.Close();
        }

        public TcpClient Connection
        {
            get
            {
                return this.tcpClient;
            }
        }

        public string IP { get { return this.ip; } }

        public int Port { get { return this.port; } }

        /// <summary>
        /// Fired when a new message was received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceived(Message<T> message)
        {
            MessageReceived?.Invoke(message);
        }

        /// <summary>
        /// Fired when a new message was received.
        /// </summary>
        public event Action<Message<T>> MessageReceived;

        /// <summary>
        /// Send a message to the server.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessageToServer(Message<T> message)
        {
            byte[] messageBytes = message.ToBytes();
            clientStream.Write(messageBytes, 0, messageBytes.Length);
        }

    }

}
