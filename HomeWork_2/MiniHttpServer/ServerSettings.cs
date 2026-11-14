using System;
using System.Collections.Generic;
using System.Text;



namespace MiniHttpServer
{
    public class ServerSettings
    {
        public int Port { get; set; }

        public string Path { get; set; } = string.Empty;

        public string PublicDirectoryPath { get; set; }
            = string.Empty;
    }
}

