using System.Net.Sockets;

namespace NetworkServer {

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

}
