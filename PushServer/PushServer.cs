using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PushServer
{
    class PushServer
    {
        Setting setting;

        TcpListener server;
        Thread acceptThread;
        Thread clientThread;

        public List<PushClient> ClientList { get; set; }

        Queue<byte[]> messages;

        public void StartServer(Setting _setting)
        {
            this.setting = _setting;

            server = new TcpListener(IPAddress.Parse(setting.Ip), setting.Port);
            server.Start();

            messages = new Queue<byte[]>();
            ClientList = new List<PushClient>();

            clientThread = new Thread(manageClient);
            clientThread.Start();

            acceptThread = new Thread(acceptLooper);
            acceptThread.Start();
        }

        // TCP 연결 수락
        void acceptLooper()
        {
            while (true)
            {
                var tcpClient = server.AcceptTcpClient();
                var pushClient = new PushClient(tcpClient);
                pushClient.SetStreamWriteTimeout(setting.StreamWriteTimeout);

                ClientList.Add(pushClient);

                log(pushClient.GetIp() + " connected");
            }
        }

        // 연결된 클라이언트 상태 확인, 메세지 일괄 전송
        void manageClient()
        {
            long pingInterval = TimeSpan.FromSeconds(setting.PingIntervalSecond).Ticks;
            long lastPingTime = DateTime.Now.Ticks;

            List<PushClient> deadClients;

            while (true)
            {
                byte[] msg = null;
                if (messages.Count > 0)
                    msg = messages.Dequeue();

                deadClients = new List<PushClient>(setting.DeadClientInitCapacity);

                bool requirePing = (DateTime.Now.Ticks - lastPingTime > pingInterval);

                foreach (var client in ClientList)
                {
                    try
                    {
                        // Check Alive
                        if (requirePing)
                        {
                            if (!client.CheckAlive())
                            {
                                deadClients.Add(client);
                                continue;
                            }
                        }
                        
                        // Send Message
                        if (msg != null)
                        {
                            bool success = client.Send(DataType.Notifycation, msg);

                            if (success)
                                log(client.GetIp() + " sent");
                            else
                                deadClients.Add(client);
                        }
                    }
                    catch (Exception ex)
                    {
                        log(ex.ToString());
                    }
                }

                // have to find more efficient way
                foreach (var deads in deadClients)
                {
                    ClientList.Remove(deads);
                    log(deads.GetIp() + " disconnected");
                }
                deadClients.Clear();

                Thread.Sleep(setting.ManageThreadInterval);

                if (requirePing) // refresh LastPingTime
                    lastPingTime = DateTime.Now.Ticks;
            }
        }

        // 메세지 전송
        public void Push(string msg)
        {
            var data = Encoding.UTF8.GetBytes(msg);
            messages.Enqueue(data);
        }

        public void Stop()
        {
            // TODO
            throw new NotImplementedException();
        }

        // 임시 로거
        void log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
