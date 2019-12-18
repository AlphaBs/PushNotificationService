using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CSPushClient
{
    public class UdpPushService
    {
        public UdpPushService(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        string ip;
        int port;

        UdpClient client;

        public void Connect()
        {
            client = new UdpClient();
            client.Connect(ip, port);

            client.Send(new byte[] { (byte)DataType.Hello }, 1);
        }

        public string WaitForNotification()
        {
            try
            {
                while (true)
                {
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
                    var buffer = client.Receive(ref ip);
                    Console.WriteLine(BitConverter.ToString(buffer));

                    var dataType = (DataType)buffer[0];
                    switch (dataType)
                    {
                        case DataType.Ping:
                            client.Send(buffer, buffer.Length);
                            break;

                        case DataType.Notification:
                            return Encoding.UTF8.GetString(buffer, 1, buffer.Length - 1);

                        default:
                            Array.Clear(buffer, 0, buffer.Length);
                            break;
                    }
                }
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
            }
            catch { }
        }
    }
}
