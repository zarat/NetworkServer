using System.ComponentModel;
using System.Net.Sockets;

namespace NetworkServer {

    /// <summary>
    /// 
    /// </summary>
    public class TCPClient
    {

        private TcpClient tcpClient;
        private Thread clientThread;
        private NetworkStream clientStream;

        private string ip;
        private int port;

        private bool running = false;

        public TCPClient(string _ip, int _port)
        {
            ip = _ip;
            port = _port;
        }

        public void Start()
        {
            tcpClient = new TcpClient();
            running = true;
            clientThread = new Thread(new ThreadStart(ConnectToServer));
            
            clientThread.Start();

            while (clientStream == null)
            {
                Thread.Sleep(10);
            }
        }

        private void ConnectToServer()
        {
            tcpClient.Connect(this.ip, this.port);
            clientStream = this.tcpClient.GetStream();

            byte[] serverResponseBuffer = new byte[4096];
            int bytesRead;

            while (running)
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

                OnMessageReceived(serverResponseBuffer);

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
        protected virtual void OnMessageReceived(byte[] message)
        {
            MessageReceived?.Invoke(message);
        }

        /// <summary>
        /// Fired when a new message was received.
        /// </summary>
        public event Action<byte[]> MessageReceived;

        /// <summary>
        /// Send a message to the server.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessageToServer(byte[] message)
        {
            clientStream.Write(message, 0, message.Length);
        }

    }

}
