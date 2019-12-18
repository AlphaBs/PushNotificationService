using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PushServer
{
    class UdpPushServer
    {
        Setting setting;

        Thread receiveThread;
        Thread manageThread;

        public bool Working { get; private set; }
        public List<UdpPushClient> ClientList { get; set; }

        Queue<byte[]> messages;

        public void Start(Setting _setting)
        {
            Working = true;

            this.setting = _setting;

            messages = new Queue<byte[]>();
            ClientList = new List<UdpPushClient>();

            manageThread = new Thread(manageClient);
            manageThread.Start();

            receiveThread = new Thread(receiveLooper);
            receiveThread.Start();
        }

        // UDP 응답 처리
        void receiveLooper()
        {
            var server = new UdpClient(setting.Port);

            while (Working)
            {
                try
                {
                    IPEndPoint clientAddress = new IPEndPoint(IPAddress.Any, 0);
                    var data = server.Receive(ref clientAddress);
                    Console.WriteLine(BitConverter.ToString(data));

                    if (clientAddress == null)
                        continue;

                    var client = registerClient(clientAddress);

                    var pre = (DataType)data[0];
                    switch (pre)
                    {
                        case DataType.Ping:
                            client.LastPingSuccess = true;
                            break;
                        case DataType.Hello:
                            UdpPushClient.Send(clientAddress, DataType.Hello, null);
                            break;
                        case DataType.Notification:
                            break;
                        default:
                            break;
                    }
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

        UdpPushClient getClient(IPEndPoint address)
        {
            foreach (var item in ClientList)
            {
                if (item.Ip == address)
                    return item;
            }

            return null;
        }

        UdpPushClient registerClient(IPEndPoint address)
        {
            var client = getClient(address);

            if (client == null)
            {
                client = new UdpPushClient(address);
                ClientList.Add(client);
                Console.WriteLine(address + " connected");
            }

            return client;
        }

        // 연결된 클라이언트 상태 확인, 메세지 일괄 전송
        void manageClient()
        {
            ConcurrentQueue<UdpPushClient> deadClients = new ConcurrentQueue<UdpPushClient>(); ;

            while (Working)
            {
                byte[] msg = null;
                if (messages.Count > 0)
                    msg = messages.Dequeue();

                try
                {
                    clientWorker((client) =>
                    {
                        var success = true;

                        if (!client.LastPingSuccess && (client.LastPingTimeSpan() > setting.PingTimeoutSecond))
                            success = false;
                        else if (client.LastPingTimeSpan() > setting.PingIntervalSecond)
                            success = client.Ping();
                        else if (msg != null)
                        {
                            success = client.Send(DataType.Notification, msg);
                            log(client.Ip.ToString() + " sent");
                        }

                        if (!success)
                            deadClients.Enqueue(client);
                    });
                }
                catch (Exception ex)
                {
                    log(ex.ToString());
                }

                // Remove disconnected clients
                while (deadClients.Count > 0)
                {
                    UdpPushClient deads;
                    if (deadClients.TryDequeue(out deads))
                    {
                        ClientList.Remove(deads);
                        log(deads.Ip + " disconnected");
                    }
                }

                Thread.Sleep(setting.ManageThreadInterval);
                //GC.Collect();
            }
        }

        // 여러 스레드에서 일괄 작업
        void clientWorker(Action<UdpPushClient> work, bool forceWork = false)
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
                        UdpPushClient client;

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

            receiveThread.Join();
            log("receiveThread closed");

            manageThread.Join();
            log("manageThread closed");

            clientWorker((client) =>
            {
                client.Send(DataType.End, null);
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
