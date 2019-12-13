﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;

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
                Ip = "127.0.0.1"
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
                        exit();
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

        static void exit()
        {
            Console.WriteLine("stopped");
            Console.ReadLine();
        }
    }
}
