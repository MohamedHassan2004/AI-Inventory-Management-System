using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities
{
    public class Category
    {
        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string ImgUrl { get; private set; } = string.Empty;

        public bool IsDeleted { get; private set; } = false;
        public List<Product> Products { get; private set; } = new();

        private Category() { } // For EF

        public Category(string name, string imgUrl)
        {
            UpdateName(name);
            UpdateImage(imgUrl);
        }

        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty");

            if (name.Length > 100)
                throw new ArgumentException("Name too long");

            Name = name.Trim();
        }

        public void UpdateImage(string imgUrl)
        {
            if (string.IsNullOrWhiteSpace(imgUrl))
                throw new ArgumentException("Invalid image url");

            ImgUrl = imgUrl.Trim();
        }

        public void Rename(string newName)
        {
            UpdateName(newName);
        }
        public void Delete()
        {
            IsDeleted = true;
        }
    }
}
