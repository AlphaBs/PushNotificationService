using System;
using CSPushClient;

namespace CSPushTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("start");

            var ip = "127.0.0.1";
            //var ip = "52.231.69.144";
            var port = 24356; // default port

            var service = new UdpPushService(ip, port);
            service.Connect();

            Console.WriteLine("connected");

            var msg = "";
            while ((msg = service.WaitForNotification()) != null)
            {
                Console.WriteLine(msg);
            }

            Console.WriteLine("disconnected");
            Console.ReadLine();
        }
    }
}
