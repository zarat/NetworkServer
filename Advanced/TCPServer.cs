using System.Net.Sockets;

namespace NetworkServer {

    /// <summary>
    /// Receive TCP clients.
    /// 
    /// Event ClientConnect is fired when a new client connects.
    /// Event ClientDisconnected is fired when a client disconnects.
    /// Event MessageReceived is fired when a message is received.
    /// Event MessageReceivedError is fired when a message could not have been received or was in a wrong format.
    /// </summary>
    /// \todo OnListenerStop event
    /// \todo Donst automatically start to listen
    /// <typeparam name="T">The type of Message it handles.</typeparam>
    public class TCPServer<T>
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
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            running = true;
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

            byte[] messageBuffer = new byte[maxReveiveSize];
            int bytesRead;

            try
            {
                while (running)
                {
                    bytesRead = clientStream.Read(messageBuffer, 0, maxReveiveSize);

                    if (bytesRead == 0)
                        break;

                    Message<T> receivedMessage = Message<T>.FromBytes(messageBuffer);

                    if (receivedMessage == null)
                    {
                        //OnMessageReceivedError("Unbekanntes Paket: " + BitConverter.ByteArrayToHexString(messageBuffer));
                        OnMessageReceivedError( messageBuffer );
                    }
                    else
                        OnMessageReceived(receivedMessage, tcpClient);
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
        protected virtual void OnMessageReceived(Message<T> message, TcpClient client)
        {
            MessageReceived?.Invoke(message, client);
        }

        /// <summary>
        /// When a message is received.
        /// </summary>
        /// <param name="message">The Message Object.</param>
        /// <param name="client">The TcpClient from which the message was received.</param>
        public event Action<Message<T>, TcpClient> MessageReceived;

        /// <summary>
        /// When a message could not have been evaluated as registered Message.
        /// </summary>
        /// \todo Change object to byte[]
        /// <param name="packet">The received packet as byte[]</param>
        protected virtual void OnMessageReceivedError(object packet)
        {
            MessageReceivedError?.Invoke(packet);
        }

        /// <summary>
        /// When a message could not have been evaluated as registered Message.
        /// </summary>
        /// \todo Change object to byte[]
        /// <param name="packet">The received packet as byte[]</param>
        public event Action<object> MessageReceivedError;

        
    }

}
