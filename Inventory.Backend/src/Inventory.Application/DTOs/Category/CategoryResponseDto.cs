using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Category
{
    public class CategoryResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ImgUrl { get; set; } = string.Empty;
    }
}
