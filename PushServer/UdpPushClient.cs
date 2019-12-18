using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

namespace PushServer
{
    class UdpPushClient
    {
        public UdpPushClient(IPEndPoint address)
        {
            this.Ip = address;
            LastPingRequestTime = DateTime.Now.Ticks;
        }

        public IPEndPoint Ip { get; private set; }
        public bool LastPingSuccess { get; set; }
        public long LastPingRequestTime { get; private set; }

        public bool Send(DataType pre, byte[] data)
        {
            return Send(Ip, pre, data);
        }

        public int LastPingTimeSpan()
        {
            return TimeSpan.FromTicks(DateTime.Now.Ticks - LastPingRequestTime).Seconds;
        }

        public bool Ping()
        {
            LastPingRequestTime = DateTime.Now.Ticks;
            LastPingSuccess = false;
            return Send(DataType.Ping, null);
        }

        public static bool Send(IPEndPoint address, DataType pre, byte[] data)
        {
            try
            {
                var client = new UdpClient();

                var sendData = new byte[] { (byte)pre };
                if (data != null)
                    sendData = sendData.Concat(data).ToArray();

                client.Send(sendData, sendData.Length, address);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("sendMessage Exception " + address.ToString() + " : " + ex.ToString());
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Ip.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Ip.GetHashCode();
        }

        public override string ToString()
        {
            return Ip.ToString();
        }
    }
}
