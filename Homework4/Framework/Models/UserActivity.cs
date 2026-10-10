using System;

namespace CustomHttpServer.Models
{
    public class UserActivity
    {
        public int Id { get; set; }
        /// <summary>Тип: login | submit | grade | message</summary>
        public string Type { get; set; } = "info";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}