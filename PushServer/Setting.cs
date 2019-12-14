using System;
using System.Collections.Generic;
using System.Text;

namespace PushServer
{
    public class Setting
    {
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 24356;
        public int ManageThreadInterval { get; set; } = 1000;
        public int PingIntervalSecond { get; set; } = 60;
        public int DeadClientInitCapacity { get; set; } = 30;
        public int StreamWriteTimeout { get; set; } = 1000;
        public int ClientThreadCount { get; set; } = 5;
    }
}
