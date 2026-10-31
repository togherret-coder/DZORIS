using System;
using System.Collections.Generic;
using System.Linq;
using KONTROLNIYA.Models; // Ссылка на модель

namespace KONTROLNIYA.Services // Пространство имен сервиса
{
    public class MaterialService
    {
        private readonly List<Material> _materials = new();
        private int _nextId = 1;
        private readonly object _lock = new();

        public Material Create(string title, string format)
        {
            Validate(title, format, "draft");

            lock (_lock)
            {
                var material = new Material
                {
                    Id = _nextId++,
                    Title = title,
                    Format = format,
                    Status = "draft"
                };
                _materials.Add(material);
                return material;
            }
        }

        public List<Material> GetFiltered(string? format, string? status)
        {
            lock (_lock)
            {
                var query = _materials.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(format))
                {
                    query = query.Where(m => m.Format.Equals(format, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(m => m.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                return query.ToList();
            }
        }

        public Material? GetById(int id)
        {
            lock (_lock)
            {
                return _materials.FirstOrDefault(m => m.Id == id);
            }
        }

        public void Update(int id, string title, string format, string status)
        {
            Validate(title, format, status);

            lock (_lock)
            {
                var material = _materials.FirstOrDefault(m => m.Id == id);
                if (material == null)
                {
                    throw new KeyNotFoundException("Материал не найден");
                }

                material.Title = title;
                material.Format = format;
                material.Status = status;
            }
        }

        private void Validate(string title, string format, string status)
        {
            if (title.Length < 3 || title.Length > 100)
                throw new ArgumentException("Название должно быть от 3 до 100 символов.");

            if (format != "pdf" && format != "video" && format != "article")
                throw new ArgumentException("Формат должен быть: pdf, video, article.");

            if (status != "draft" && status != "published")
                throw new ArgumentException("Статус должен быть: draft, published.");
        }
    }
}