using System.ComponentModel;
using System.Net.Sockets;

namespace NetworkServer {

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

}
