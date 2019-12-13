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
        Test = 0xFF,
        Notifycation = 1
    }

    class PushClient
    {

        public PushClient(TcpClient tcp)
        {
            this.client = tcp;
            this.ns = tcp.GetStream();

            ip = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
        }

        string ip;
        TcpClient client;
        NetworkStream ns;

        public void SetStreamWriteTimeout(int value)
        {
            ns.WriteTimeout = value;
        }

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

                if (sendData.Length > 1024)
                {
                    byte[] tempArr = new byte[1024];
                    Array.Copy(sendData, 0, tempArr, 0, 1024);
                    sendData = tempArr;
                }

                ns.Write(sendData, 0, sendData.Length);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public bool CheckAlive()
        {
            try
            {
                return Send(DataType.Test, null);
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException ||
                    ex is IOException)
                    return false;
                else
                    throw ex;
            }
        }

        public string GetIp()
        {
            return ip;
        }

        public void Close()
        {
            ns.Dispose();
            ns = null;

            client.Close();
            client = null;
        }
    }
}
