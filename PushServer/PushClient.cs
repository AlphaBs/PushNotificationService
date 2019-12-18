using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

namespace PushServer
{
    public enum DataType
    {
        Ping          = 0xFF,
        Hello         = 0,
        Notification  = 1,
        End           = 2
    }

    class PushClient
    {
        public PushClient(TcpClient tcp)
        {
            this.client = tcp;
            this.ns = tcp.GetStream();

            Ip = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
            Connected = true;
        }

        public string Ip { get; private set; }
        public int StreamReadTimeout { get => ns.ReadTimeout; set => ns.ReadTimeout = value; }
        public int StreamWriteTimeout { get => ns.WriteTimeout; set => ns.WriteTimeout = value; }
        public bool Connected { get; private set; }

        TcpClient client;
        NetworkStream ns;

        public bool Send(DataType pre, byte[] data)
        {
            return Send((byte)pre, data);
        }

        public bool Send(byte pre, byte[] data)
        {
            try
            {
                var sendData = new byte[] { pre };
                if (data != null)
                    sendData = sendData.Concat(data).ToArray();

                if (sendData.Length > 256)
                {
                    byte[] tempArr = new byte[256];
                    Array.Copy(sendData, 0, tempArr, 0, 256);
                    sendData = tempArr;
                }

                ns.Write(sendData, 0, sendData.Length);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException ||
                    ex is ObjectDisposedException ||
                    ex is IOException)
                    return false;
                else
                    throw ex;
            }
        }

        public bool CheckAlive()
        {
            var success = Send(DataType.Ping, null);

            if (!success)
            {
                Connected = false;
                return false;
            }

            try
            {
                var response = ns.ReadByte();
                return (response == (int)DataType.Ping);
            }
            catch (IOException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public void Close()
        {
            try
            {
                if (ns != null)
                {
                    ns.Dispose();
                    ns = null;
                }

                client.Dispose();
                client = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Connected = false;
        }
    }
}
