using System.ComponentModel;
using System.Net.Sockets;

namespace NetworkServer {

    /// <summary>
    /// 
    /// </summary>
    public class TCPServer
    {

        /// <summary>
        /// The listening socket
        /// </summary>
        private TcpListener tcpListener;

        /// <summary>
        /// The listening socket runs on its own thread.
        /// </summary>
        private Thread listenerThread;

        /// <summary>
        /// A list of all connected clients.
        /// </summary>
        private List<TcpClient> connectedClients = new List<TcpClient>();

        /// <summary>
        /// The running port
        /// </summary>
        private int port;

        /// <summary>
        /// If the server is running.
        /// </summary>
        private bool running;

        /// <summary>
        /// The maximum receive size for a packet.
        /// </summary>
        public int maxReveiveSize = 4096;

        /// <summary>
        /// The maximum size of connected clients.
        /// </summary>
        public int maxClientSize = 8;

        /// <summary>
        /// Create a TCP Server on the specified port.
        /// </summary>
        /// <param name="_port">The port to listen on.</param>
        public TCPServer(int _port)
        {
            port = _port;
        }

        /// <summary>
        /// Start listening for clients.
        /// </summary>
        public void Start()
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            running = true;
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            
            listenerThread.Start();
        }

        /// <summary>
        /// Just set running to false.
        /// </summary>
        /// \todo Anything else here?
        public void Stop()
        {
            running = false;
        }

        /// <summary>
        /// Runs on its own thread. Listen for new clients. 
        /// When a new client connects it raises the event OnClientConnect. 
        /// If a client disconnects it raises the event OnClientDisconnect.
        /// </summary>
        /// \todo Exception? SocketTimeout
        private void ListenForClients()
        {
            tcpListener.Start();

            while (running)
            {
                if (!tcpListener.Pending())
                {
                    continue;
                }

                TcpClient client = tcpListener.AcceptTcpClient();

                connectedClients.Add(client);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
                OnClientConnect(client);
            }

            tcpListener.Stop();
        }

        /// <summary>
        /// Runs on its own thread. Handles the communication with the client. When a message is received it raises the event OnMessageReceived.
        /// If the received data was not a registered struct or some random bytes it raises the event OnMessageReceivedError.
        /// </summary>
        /// <param name="clientObj"></param>
        private void HandleClient(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] messageBuffer;
            int bytesRead;

            try
            {
                while (running)
                {

                    messageBuffer = new byte[maxReveiveSize];

                    bytesRead = clientStream.Read(messageBuffer, 0, maxReveiveSize);

                    if (bytesRead == 0)
                        break;

                    if (messageBuffer == null)
                    {
                        OnMessageReceivedError(messageBuffer);
                    }
                    else
                        OnMessageReceived(messageBuffer, tcpClient);
                }
            }
            catch
            {
                // Handle exceptions as needed
            }
            finally
            {
                // Close the client stream and wait for it to be fully closed
                clientStream.Close();
                tcpClient.Close();

                // Ensure that the client is fully disconnected before proceeding
                OnClientDisconnect(tcpClient);

                connectedClients.Remove(tcpClient);
            }
        }

        /// <summary>
        /// When a new client connects
        /// </summary>
        /// <param name="client">The TcpClient that has connected.</param>
        protected virtual void OnClientConnect(TcpClient client)
        {
            ClientConnect?.Invoke(client);
        }

        /// <summary>
        /// When a new client connects
        /// </summary>
        /// <param name="client">The TcpClient that has connected.</param>
        public event Action<TcpClient> ClientConnect;

        /// <summary>
        /// When a client disconnects
        /// </summary>
        /// <param name="client">The TcpClient that has disconnected.</param>
        protected virtual void OnClientDisconnect(TcpClient client)
        {
            ClientDisconnect?.Invoke(client);
        }

        /// <summary>
        /// When a client disconnects
        /// </summary>
        /// <param name="client">The TcpClient that has disconnected.</param>
        public event Action<TcpClient> ClientDisconnect;

        /// <summary>
        /// When a message is received.
        /// </summary>
        /// <param name="message">The Message Object.</param>
        /// <param name="client">The TcpClient from which the message was received.</param>
        protected virtual void OnMessageReceived(byte[] message, TcpClient client)
        {
            MessageReceived?.Invoke(message, client);
        }

        /// <summary>
        /// When a message is received.
        /// </summary>
        public event Action<byte[], TcpClient> MessageReceived;

        /// <summary>
        /// When a message could not have been evaluated as registered Message.
        /// </summary>
        /// <param name="packet">The received packet as byte[]</param>
        protected virtual void OnMessageReceivedError(byte[] packet)
        {
            MessageReceivedError?.Invoke(packet);
        }

        /// <summary>
        /// When a message could not have been evaluated as registered Message.
        /// </summary>
        public event Action<byte[]> MessageReceivedError;

    }

}
