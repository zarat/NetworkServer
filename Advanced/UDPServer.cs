using System.Net;
using System.Net.Sockets;

namespace NetworkServer {

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
        protected virtual void OnMessageReceivedError(object packet)
        {
            MessageReceivedError?.Invoke(packet);
        }

        public event Action<object> MessageReceivedError;

    }

}
