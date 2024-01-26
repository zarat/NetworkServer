# NetworkServer

A simple client/server library for Unity3D, Godot or any .NET application.

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

void MyMessageReceived(Message, message) { }
```

```CSharp
UDPClient client = new UDPClient();
client.SendMessage(new Message(MessageType.String, "There is no place like ..."), "127.0.0.1", 1337);
```
