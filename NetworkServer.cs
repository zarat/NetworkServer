using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetworkServer
{

    /// <summary>
    /// A custom message object
    /// </summary>
    /// <typeparam name="T">The type it has.</typeparam>
    [Serializable]
    public class Message<T>
    {

        public T Content { get; set; }

        //public Message(string type, T content)
        public Message(T content)
        {
            //Type = type;
            Content = content;
        }

        private bool IsValidStructType()
        {
            Type contentType = typeof(T);
            List<Type> registeredStructs = MessageTypeRegistry.Instance.GetRegisteredMessageTypes();

            return registeredStructs.Contains(contentType);
        }

        public byte[] ToBytes()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static Message<T> FromBytes(byte[] data)
        {
            Message<T> message;

            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    message = (Message<T>)formatter.Deserialize(stream);

                    if (!message.IsValidStructType())
                    {
                        throw new InvalidOperationException($"Unregistered struct type: {typeof(T)}");
                    }
                }
            }
            catch (Exception e)
            {

                message = null; 

            }

            return message;
        }

    }

    /// <summary>
    /// Register message types.
    /// </summary>
    public class MessageTypeRegistry
    {

        /// <summary>
        /// A list of registered message types
        /// </summary>
        private List<Type> registeredMessageTypes;

        /// <summary>
        /// Make it a singleton
        /// </summary>
        private static MessageTypeRegistry instance;

        /// <summary>
        /// Get the instance
        /// </summary>
        public static MessageTypeRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageTypeRegistry();
                }
                return instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// \todo Standard types?
        private MessageTypeRegistry()
        {
            registeredMessageTypes = new List<Type>();
            RegisterMessageType(typeof(Vector3));
        }

        /// <summary>
        /// Register a message type.
        /// </summary>
        /// <param name="structType"></param>
        /// <exception cref="ArgumentException"></exception>
        public void RegisterMessageType(Type structType)
        {
            if (structType.IsValueType && !structType.IsPrimitive)
            {
                registeredMessageTypes.Add(structType);
            }
            else
            {
                //throw new ArgumentException($"{structType.FullName} is not a valid message type.");
            }
        }

        /// <summary>
        /// Get a list of registered message types
        /// </summary>
        /// <returns></returns>
        public List<Type> GetRegisteredMessageTypes()
        {
            return registeredMessageTypes;
        }
    
    }

    /// <summary>
    /// 
    /// </summary>
    public class UDPServer
    {

        private UdpClient udpServer;
        private int port;
        private Thread receiveThread;

        private bool running;

        public UDPServer(int _port)
        {
            port = _port;
        }

        public void Start()
        {
            udpServer = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            running = true;
            receiveThread.Start();
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        /// \todo How to properly stop it?
        public void Stop()
        {
            running = false;

            udpServer.Close();

            receiveThread.Join();
        }

        /// <summary>
        /// The event OnMessageReceived is fired when a valid Message is received.
        /// The event OnMessageReceiveError is fired when some random data was received.
        /// </summary>
        private void ReceiveData()
        {

            try
            {

                while (running)
                {

                    IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpServer.Receive(ref clientEndPoint);

                    if (data == null)
                    {
                        OnMessageReceivedError(data);
                    }
                    else
                        OnMessageReceived(data);

                }

            }
            catch (SocketException)
            {
                // Ignore timeout exception, check the running flag, and continue the loop
            }

        }

        /// <summary>
        /// This event gets fired when a new message is received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceived(byte[] message)
        {
            MessageReceived?.Invoke(message);
        }

        public event Action<byte[]> MessageReceived;

        /// <summary>
        /// This event gets fired when data is received but its not in a valid format.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceivedError(byte[] packet)
        {
            MessageReceivedError?.Invoke(packet);
        }

        public event Action<byte[]> MessageReceivedError;

    }

    /// <summary>
    /// 
    /// </summary>
    public class UDPClient
    {
        private UdpClient udpClient;

        public UDPClient()
        {
            udpClient = new UdpClient();
        }

        public void SendMessage(byte[] message, string serverIp, int serverPort)
        {
            udpClient.Send(message, message.Length, serverIp, serverPort);
        }
    }

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

            byte[] messageBuffer = new byte[maxReveiveSize];
            int bytesRead;

            try
            {
                while (running)
                {
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

    /// <summary>
    /// Receive UDP messages.
    /// Event OnMessageReceived is fired when a message is received.
    /// Event OnMessageReceivedError is fired when a message could not have been received or was in a wrong format.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UDPServer<T>
    {

        private UdpClient udpServer;
        private int port;
        private Thread receiveThread;

        private bool running;

        public UDPServer(int _port)
        {
            port = _port;
        }

        public void Start()
        {
            udpServer = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            running = true;
            receiveThread.Start();
        }

        /// <summary>
        /// Stop listening
        /// </summary>
        /// \todo How to properly stop it?
        public void Stop()
        {
            running = false;

            udpServer.Close();

            receiveThread.Join();
        }

        /// <summary>
        /// The event OnMessageReceived is fired when a valid Message is received.
        /// The event OnMessageReceiveError is fired when some random data was received.
        /// </summary>
        private void ReceiveData()
        {

            try
            {

                while (running)
                {

                    IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpServer.Receive(ref clientEndPoint);

                    Message<T> receivedMessage = Message<T>.FromBytes(data);

                    if (receivedMessage == null)
                    {
                        OnMessageReceivedError(data);
                    }
                    else
                        OnMessageReceived(receivedMessage);

                }

            }
            catch (SocketException)
            {
                // Ignore timeout exception, check the running flag, and continue the loop
            }

        }

        /// <summary>
        /// This event gets fired when a new message is received.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceived(Message<T> message)
        {
            MessageReceived?.Invoke(message);
        }

        public event Action<Message<T>> MessageReceived;

        /// <summary>
        /// This event gets fired when data is received but its not in a valid format.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceivedError(byte[] packet)
        {
            MessageReceivedError?.Invoke(packet);
        }

        public event Action<byte[]> MessageReceivedError;

    }

    /// <summary>
    /// Send a UDP message.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UDPClient<T>
    {
        private UdpClient udpClient;

        public UDPClient()
        {
            udpClient = new UdpClient();
        }

        public void SendMessage(Message<T> message, string serverIp, int serverPort)
        {
            byte[] data = message.ToBytes();
            udpClient.Send(data, data.Length, serverIp, serverPort);
        }
    }

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
        protected virtual void OnMessageReceivedError(byte[] packet)
        {
            MessageReceivedError?.Invoke(packet);
        }

        /// <summary>
        /// When a message could not have been evaluated as registered Message.
        /// </summary>
        /// \todo Change object to byte[]
        /// <param name="packet">The received packet as byte[]</param>
        public event Action<byte[]> MessageReceivedError;

        
    }

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

        private bool running = false;

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
