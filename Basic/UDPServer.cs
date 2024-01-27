using System.Net;
using System.Net.Sockets;

namespace NetworkServer {

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

}
