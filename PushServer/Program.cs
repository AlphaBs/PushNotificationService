using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace PushServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server");

            var p = new PushServer();
            p.StartServer(new Setting()
            {
                Ip = "0.0.0.0"
            });

            Console.WriteLine("Started");
            Console.WriteLine("/stop : exit program");
            Console.WriteLine("/list : show connections");
            Console.WriteLine("input message :");

            while (true)
            {
                var msg = Console.ReadLine();

                switch (msg)
                {
                    case "/stop":
                        Console.WriteLine("stopping server");
                        p.Stop();
                        exit();
                        break;

                    case "/thread":
                        threads();
                        break;

                    case "/list":
                        showClients(p);
                        break;

                    case "/gc": // ONLY TO DEBUG
                        GC.Collect();
                        break;

                    default:
                        p.Push(msg);
                        break;
                }
            }
        }

        static void showClients(PushServer p)
        {
            Console.WriteLine("There are {0} clients : ", p.ClientList.Count);

            foreach (var item in p.ClientList)
            {
                Console.WriteLine(item.GetIp());
            }
        }

        static void threads()
        {
            ProcessThreadCollection currentThreads = Process.GetCurrentProcess().Threads;

            foreach (ProcessThread thread in currentThreads)
            {
                Console.WriteLine("{0}: {1}", thread.Id, thread.ThreadState);
            }
        }

        static void exit()
        {
            Console.WriteLine("stopped");
            Console.ReadLine();

            Environment.Exit(0);
        }
    }
}
