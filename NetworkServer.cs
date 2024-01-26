using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetworkServer
{

    public enum MessageType
    {
        String,
        Int,
        Float,
        Vector3
    }

    [Serializable]
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    [Serializable]
    public class Message
    {

        public MessageType Type { get; set; }
        public string Content { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public Vector3 VectorValue { get; set; }

        public Message(MessageType type, string content = "", int intValue = 0, float floatValue = 0.0f, Vector3 vectorValue = default(Vector3))
        {
            Type = type;
            Content = content;
            IntValue = intValue;
            FloatValue = floatValue;
            VectorValue = vectorValue;
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

        public static Message FromBytes(byte[] data)
        {
            Message message;

            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    message = (Message)formatter.Deserialize(stream);
                }
            }
            catch(Exception e) 
            {
                message = new Message(MessageType.String, "Fehlerhafte Nachricht");
            }

            return message;
        }
    }

    [Serializable]
    public class Message<T>
    {
        public T Content { get; set; }
        public MessageType Type { get; set; }

        public Message(MessageType type, T content)
        {
            Type = type;
            Content = content;
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
                }
            }
            catch (Exception e)
            {
                // Handle deserialization error
                message = new Message<T>(MessageType.String, default(T));
            }

            return message;
        }
    }

    public class UDPServer
    {

        private UdpClient udpServer;
        private Thread receiveThread;

        private bool running;

        public UDPServer(int port)
        {
            udpServer = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            running = true;
            receiveThread.Start();
        }

        public void Stop()
        {
            running = false;

            udpServer.Close();

            receiveThread.Join();
        }


        private void ReceiveData()
        {

            while (running)
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref clientEndPoint);
                Message receivedMessage = Message.FromBytes(data);
                OnMessageReceived(receivedMessage);
            }

        }

        protected virtual void OnMessageReceived(Message message)
        {
            MessageReceived?.Invoke(message);
        }

        public event Action<Message> MessageReceived;

    }

    public class UDPClient
    {
        private UdpClient udpClient;

        public UDPClient()
        {
            udpClient = new UdpClient();
        }

        public void SendMessage(Message message, string serverIp, int serverPort)
        {
            byte[] data = message.ToBytes();
            udpClient.Send(data, data.Length, serverIp, serverPort);
        }
    }

    public class TCPServer
    {

        private TcpListener tcpListener;
        private Thread listenerThread;
        private List<TcpClient> connectedClients = new List<TcpClient>();
        private int port;
        private bool running;

        public TCPServer(int _port)
        {
            port = _port;
            tcpListener = new TcpListener(IPAddress.Any, _port);
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            running = true;
            listenerThread.Start();
        }

        private void ListenForClients()
        {

            tcpListener.Start();

            while (running)
            {
                if (!tcpListener.Pending())
                {
                    continue;
                }

                TcpClient client = tcpListener.AcceptTcpClient();  // Stelle sicher, dass tcpListener.Stop() nicht aufgerufen wird, wenn dieser Aufruf blockiert
                OnClientConnect(client);
                connectedClients.Add(client);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }

            tcpListener.Stop();

        }

        public void StopListening()
        {

            running = false;

            // Stoppe den Listener-Thread
            tcpListener.Stop();

            // Warte darauf, dass der Listener-Thread endet
            listenerThread.Join();

            // Schließe alle verbundenen Clients
            foreach (TcpClient client in connectedClients)
            {
                client.Close();
            }


        }

        private void HandleClientComm(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] messageBuffer = new byte[4096];
            int bytesRead;

            while (running)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(messageBuffer, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                Message receivedMessage = Message.FromBytes(messageBuffer);
                OnMessageReceived(receivedMessage, tcpClient);

                /*
                Message responseMessage = new Message(MessageType.String, "Server has received your message: " + receivedMessage.Content);
                SendMessageToClient(tcpClient, responseMessage);
                */

            }

            connectedClients.Remove(tcpClient);
            OnClientDisconnect(tcpClient);
            tcpClient.Close();
        }

        private void SendMessageToClient(TcpClient client, Message message)
        {
            NetworkStream clientStream = client.GetStream();
            byte[] messageBytes = message.ToBytes();
            clientStream.Write(messageBytes, 0, messageBytes.Length);
        }

        public void SendMessageToAllClients(Message message)
        {
            byte[] messageBytes = message.ToBytes();
            foreach (TcpClient client in connectedClients)
            {
                NetworkStream clientStream = client.GetStream();
                clientStream.Write(messageBytes, 0, messageBytes.Length);
            }
        }

        public void SendMessageToAllClientsExceptSender(Message message, TcpClient sender)
        {
            byte[] messageBytes = message.ToBytes();
            foreach (TcpClient client in connectedClients)
            {
                if (client == sender)
                    continue;
                NetworkStream clientStream = client.GetStream();
                clientStream.Write(messageBytes, 0, messageBytes.Length);
            }
        }

        public void StartListening()
        {
            // Implement your game logic here for the server
        }

        protected virtual void OnClientConnect(TcpClient client)
        {
            ClientConnect?.Invoke(client);
        }

        public event Action<TcpClient> ClientConnect;

        protected virtual void OnMessageReceived(Message message, TcpClient client)
        {
            MessageReceived?.Invoke(message, client);
        }

        public event Action<Message, TcpClient> MessageReceived;

        protected virtual void OnClientDisconnect(TcpClient client)
        {
            ClientDisconnect?.Invoke(client);
        }

        public event Action<TcpClient> ClientDisconnect;

    }

    public class TCPClient
    {

        private TcpClient tcpClient;
        private Thread clientThread;
        private NetworkStream clientStream;

        private string ip;
        private int port;

        public TCPClient(string ip, int port)
        {
            this.tcpClient = new TcpClient();
            this.clientThread = new Thread(new ThreadStart(ConnectToServer));
            this.clientThread.Start();
            this.ip = ip;
            this.port = port;
        }

        private void ConnectToServer()
        {
            this.tcpClient.Connect(this.ip, this.port);
            this.clientStream = this.tcpClient.GetStream();

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

                Message serverResponse = Message.FromBytes(serverResponseBuffer);
                OnMessageReceived(serverResponse);

            }
        }

        public void Close()
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

        protected virtual void OnMessageReceived(Message message)
        {
            MessageReceived?.Invoke(message);
        }

        public event Action<Message> MessageReceived;

        // Methode zum Senden von Nachrichten an den Server
        public void SendMessageToServer(Message message)
        {
            byte[] messageBytes = message.ToBytes();
            clientStream.Write(messageBytes, 0, messageBytes.Length);
        }

    }

    public class UDPServer<T>
    {

        private UdpClient udpServer;
        private Thread receiveThread;

        private bool running;

        public UDPServer(int port)
        {
            udpServer = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            running = true;
            receiveThread.Start();
        }

        public void Stop()
        {
            running = false;

            udpServer.Close();

            receiveThread.Join();
        }


        private void ReceiveData()
        {

            while (running)
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref clientEndPoint);
                Message<T> receivedMessage = Message<T>.FromBytes(data);
                OnMessageReceived(receivedMessage);
            }

        }

        protected virtual void OnMessageReceived(Message<T> message)
        {
            MessageReceived?.Invoke(message);
        }

        public event Action<Message<T>> MessageReceived;

    }

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

    public class TCPServer<T>
    {
        private TcpListener tcpListener;
        private Thread listenerThread;
        private List<TcpClient> connectedClients = new List<TcpClient>();
        private int port;
        private bool running;

        public TCPServer(int _port)
        {
            port = _port;
            tcpListener = new TcpListener(System.Net.IPAddress.Any, _port);
            listenerThread = new Thread(new ThreadStart(ListenForClients));
            running = true;
            listenerThread.Start();
        }

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

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }

            tcpListener.Stop();
        }

        // Rest des TCPServer-Codes bleibt unverändert ...

        private void HandleClientComm(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] messageBuffer = new byte[4096];
            int bytesRead;

            while (running)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clientStream.Read(messageBuffer, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break;

                Message<T> receivedMessage = Message<T>.FromBytes(messageBuffer);
                OnMessageReceived(receivedMessage, tcpClient);

                /*
                Message responseMessage = new Message(MessageType.String, "Server has received your message: " + receivedMessage.Content);
                SendMessageToClient(tcpClient, responseMessage);
                */

            }

            connectedClients.Remove(tcpClient);
            ClientDisconnect(tcpClient);
            tcpClient.Close();
        }

        protected virtual void OnClientDisconnect(TcpClient client)
        {
            ClientDisconnect?.Invoke(client);
        }

        public event Action<TcpClient> ClientDisconnect;

        protected virtual void OnMessageReceived(Message<T> message, TcpClient client)
        {
            MessageReceived?.Invoke(message, client);
        }

        public event Action<Message<T>, TcpClient> MessageReceived;
    }

    public class TCPClient<T>
    {

        private TcpClient tcpClient;
        private Thread clientThread;
        private NetworkStream clientStream;

        private string ip;
        private int port;

        public TCPClient(string ip, int port)
        {
            this.tcpClient = new TcpClient();
            this.clientThread = new Thread(new ThreadStart(ConnectToServer));
            this.clientThread.Start();
            this.ip = ip;
            this.port = port;
        }

        private void ConnectToServer()
        {
            this.tcpClient.Connect(this.ip, this.port);
            this.clientStream = this.tcpClient.GetStream();

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

        public void Close()
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

        protected virtual void OnMessageReceived(Message<T> message)
        {
            MessageReceived?.Invoke(message);
        }

        public event Action<Message<T>> MessageReceived;

        // Methode zum Senden von Nachrichten an den Server
        public void SendMessageToServer(Message<T> message)
        {
            byte[] messageBytes = message.ToBytes();
            clientStream.Write(messageBytes, 0, messageBytes.Length);
        }

    }

}
