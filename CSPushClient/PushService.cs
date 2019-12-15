using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace CSPushClient
{
    public enum DataType
    {
        Ping = 0xFF,
        Notification = 0x01
    }

    public class PushService
    {
        public PushService(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }


        string ip;
        int port;

        TcpClient client;
        NetworkStream ns;

        public void Connect()
        {
            client = new TcpClient();
            client.Connect(ip, port);
            ns = client.GetStream();
        }

        public string WaitForNotification()
        {
            try
            {
                var buffer = new byte[1024];
                int nbytes;

                while ((nbytes = ns.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var dataType = (DataType)buffer[0];
                    switch (dataType)
                    {
                        case DataType.Ping:
                            ns.Write(buffer, 0, 1);
                            break;

                        case DataType.Notification:
                            return Encoding.UTF8.GetString(buffer, 1, nbytes - 1);

                        default:
                            Array.Clear(buffer, 0, buffer.Length);
                            break;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
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
