using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Category
{
    public class UpdateCategoryImageDto
    {
        public IFormFile Image { get; set; } = default!;
    }
}
