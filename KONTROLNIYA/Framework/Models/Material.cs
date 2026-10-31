using System;

namespace KONTROLNIYA.Models
{
    public class Material
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}