using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CSPushTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("connect");

            var ip = "127.0.0.1";
            var port = 24356;

            var client = new TcpClient(ip, port);
            var ns = client.GetStream();

            try
            {
                var buffer = new byte[1024];
                int nbytes;

                while ((nbytes =ns.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (buffer[0] == 1)
                    {
                        var msg = Encoding.UTF8.GetString(buffer, 1, nbytes - 1);
                        Console.WriteLine(msg);
                    }
                    else
                        Console.WriteLine(buffer[0]);
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                try
                {
                    client.Close();
                    ns.Dispose();
                } catch { }
            }

            Console.WriteLine("disconnected");
            Console.ReadLine();
        }
    }
}
