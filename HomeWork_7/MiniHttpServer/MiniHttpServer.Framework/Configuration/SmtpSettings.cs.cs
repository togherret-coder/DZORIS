
using System;


namespace MiniHttpServer.Framework.Configuration
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "smtp.mail.ru";

        public int Port { get; set; } = 465;

        public string Username { get; set; } = "";

        public string Password { get; set; } = "";
    }
}

