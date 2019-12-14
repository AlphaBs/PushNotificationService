using System;
using System.Collections.Concurrent;
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
        Thread manageThread;

        public bool Working { get; private set; }
        public List<PushClient> ClientList { get; set; }

        Queue<byte[]> messages;

        public void StartServer(Setting _setting)
        {
            Working = true;

            this.setting = _setting;

            server = new TcpListener(IPAddress.Parse(setting.Ip), setting.Port);
            server.Start();

            messages = new Queue<byte[]>();
            ClientList = new List<PushClient>();

            manageThread = new Thread(manageClient);
            manageThread.Start();

            acceptThread = new Thread(acceptLooper);
            acceptThread.Start();
        }

        // TCP 연결 수락
        void acceptLooper()
        {
            while (Working)
            {
                try
                {
                    var tcpClient = server.AcceptTcpClient();
                    var pushClient = new PushClient(tcpClient);
                    pushClient.SetStreamWriteTimeout(setting.StreamWriteTimeout);

                    lock (ClientList)
                    {
                        ClientList.Add(pushClient);
                    }

                    log(pushClient.GetIp() + " connected");
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        return;

                    log("acceptThread SocketException " + ex.SocketErrorCode.ToString() + " : " + ex.ToString());
                }
                catch(Exception ex)
                {
                    log("acceptThread Exception : " + ex.ToString());
                }
            }
        }

        // 연결된 클라이언트 상태 확인, 메세지 일괄 전송
        void manageClient()
        {
            long pingInterval = TimeSpan.FromSeconds(setting.PingIntervalSecond).Ticks;
            long lastPingTime = DateTime.Now.Ticks;

            ConcurrentQueue<PushClient> deadClients = new ConcurrentQueue<PushClient>(); ;

            while (Working)
            {
                byte[] msg = null;
                if (messages.Count > 0)
                    msg = messages.Dequeue();

                bool requirePing = (DateTime.Now.Ticks - lastPingTime > pingInterval);

                try
                {
                    // Check Alive
                    if (requirePing)
                    {
                        clientWorker((client) =>
                        {
                            if (!client.CheckAlive())
                            {
                                deadClients.Enqueue(client);
                            }
                        });

                        lastPingTime = DateTime.Now.Ticks;
                    }

                    // Send Message
                    if (msg != null)
                    {
                        clientWorker((client) =>
                        {
                            bool success = client.Send(DataType.Notifycation, msg);

                            if (success)
                                log(client.GetIp() + " sent");
                            else
                                deadClients.Enqueue(client);
                        });
                    }
                }
                catch (Exception ex)
                {
                    log(ex.ToString());
                }

                // Remove disconnected clients
                while (deadClients.Count > 0)
                {
                    PushClient deads;
                    if (deadClients.TryDequeue(out deads))
                    {
                        ClientList.Remove(deads);
                        log(deads.GetIp() + " disconnected");
                    }
                }

                Thread.Sleep(setting.ManageThreadInterval);
                //GC.Collect();
            }
        }

        // 여러 스레드에서 일괄 작업
        void clientWorker(Action<PushClient> work, bool forceWork = false)
        {
            int count = ClientList.Count;
            int lastIndex = count - 1;

            var threadCount = setting.ClientThreadCount;

            if (count <= 0)
                return;
            else if (count < threadCount)
                threadCount = count;

            var threads = new Thread[threadCount];
            for (int i = 0; i < threads.Length; i++)
            {
                var th = new Thread(new ThreadStart(delegate
                {
                    while (Working || forceWork)
                    {
                        PushClient client;

                        lock (ClientList)
                        {
                            if (lastIndex < 0)
                                break;

                            client = ClientList[lastIndex];
                            lastIndex--;
                        }

                        work(client);
                    }
                }));

                th.Start();
                threads[i] = th;
            }

            foreach (var th in threads)
            {
                th.Join();
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
            Working = false;

            server.Stop();
            log("TcpListener closed");

            acceptThread.Join();
            log("acceptThread closed");

            manageThread.Join();
            log("manageThread closed");

            clientWorker((client) =>
            {
                client.Close();
            }, true);
            log("All connection closed");
        }

        // 임시 로거
        void log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
