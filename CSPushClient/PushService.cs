using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CSPushClient
{
    public class PushService
    {
        public PushService(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public event EventHandler<string> Notice;

        string ip;
        int port;

        bool readingStream = false;

        TcpClient client;
        NetworkStream ns;

        public void Connect()
        {
            client = new TcpClient();
            client.Connect(ip, port);
            ns = client.GetStream();
        }

        public string WaitForNotifycation()
        {
            readingStream = true;
            string msg;

            try
            {
                var buffer = new byte[1024];
                int nbytes;

                while ((nbytes = ns.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (buffer[0] == 1)
                    {
                        msg = Encoding.UTF8.GetString(buffer, 1, nbytes - 1);
                        break;
                    }

                    Array.Clear(buffer, 0, buffer.Length);
                }

                msg = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                msg = null;
            }

            readingStream = false;
            return msg;
        }

        public void Close()
        {
            try
            {
                client.Close();
                ns.Dispose();
            }
            catch { }
        }
    }
}
