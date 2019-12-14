using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CSPushClient;

namespace CSPushTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("connect");

            var ip = "127.0.0.1";
            var port = 24356; // default port

            var service = new PushService(ip, port);
            var msg = "";
            while ((msg = service.WaitForNotifycation()) != null)
            {
                Console.WriteLine(msg);
            }

            Console.WriteLine("disconnected");
            Console.ReadLine();
        }
    }
}
