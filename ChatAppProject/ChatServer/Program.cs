using System;
using System.ServiceModel;
using SharedChat;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Chat Server....");

            var tcp = new NetTcpBinding();

            var host = new ServiceHost(typeof(ChatService));

            host.AddServiceEndpoint(typeof(SharedChat.ChatServiceInterface), tcp,
                "net.tcp://0.0.0.0:9000/ChatService");

            host.AddServiceEndpoint(typeof(DuplexChatService), tcp,
                "net.tcp://0.0.0.0:9000/DuplexChatService");

            host.Open();

            Console.WriteLine("System Online");
            Console.ReadLine();

            host.Close();
        }
    }
}
