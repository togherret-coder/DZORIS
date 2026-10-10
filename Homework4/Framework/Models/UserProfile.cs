using System;

namespace CustomHttpServer.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Group { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
    }
}