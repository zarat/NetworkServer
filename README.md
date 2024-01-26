# NetworkServer

A simple client/server library for Unity3D, Godot or any .NET application.

## Basic usage

```CSharp
TCPServer server = new TCPServer(1337);

server.ClientConnect += MyClientConnect;
server.ClientDisconnect += MyClientDisconnect;
server.MessageReceived += MyMessageReceived;

void MyClientConnect(TcpClient client) { }

void MyClientDisconnect(TcpClient client) { }

void MyMessageReceived(Message message, TcpClient client) { }

server.StartListening();

server.StopListening();
```

```CSharp
TCPClient client = new TCPClient("127.0.0.1", 1337);
client.MessageReceived += MyMessageReceived;

void MyMessageReceived(Message message) { }

client.Close();
```

```CSharp
UDPServer server = new UDPServer(1337);
server.MessageReceived += MyMessageReceived;

void MyMessageReceived(Message message) { }
```

```CSharp
UDPClient client = new UDPClient();
client.SendMessage(new Message(MessageType.String, "There is no place like ..."), "127.0.0.1", 1337);
```

## Custom messages

```CSharp
using System.Net.Sockets;

using NetworkServer;

namespace Test
{

    [Serializable]
    struct MyMessage
    {

        public int i;

        public MyMessage(int _i)
        {
            i = _i;
        }

    }

    class Test
    {

        static TCPServer<MyMessage> tcpServer;
        static TCPClient<MyMessage> tcpClient;

        static UDPServer<MyMessage> udpServer;
        static UDPClient<MyMessage> udpClient;

        static Message<MyMessage> msg; 

        public static void Main()
        {

            tcpServer = new TCPServer<MyMessage>(1337);
            tcpServer.MessageReceived += ServerMessageReceived;

            tcpClient = new TCPClient<MyMessage>("127.0.0.1", 1337);
            tcpClient.MessageReceived += ClientMessageReceived;

            udpServer = new UDPServer<MyMessage>(1337);
            udpServer.MessageReceived += ClientMessageReceived;

            udpClient = new UDPClient<MyMessage>();

            msg = new Message<MyMessage>(MessageType.Int, new MyMessage(13));

            tcpClient.SendMessageToServer(msg);
            udpClient.SendMessage(msg, "127.0.0.1", 1337);

        }

        static void ServerMessageReceived(Message<MyMessage> message, TcpClient client)
        {
            Console.WriteLine(message.Content.i.ToString()); 
        }

        static void ClientMessageReceived(Message<MyMessage> message)
        {
            Console.WriteLine(message.Content.i.ToString());
        }

    }

}
```
