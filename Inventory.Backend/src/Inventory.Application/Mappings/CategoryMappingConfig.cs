using Inventory.Application.DTOs;
using Inventory.Application.DTOs.Category;
using Inventory.Domain.Entities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Mappings;

public class CategoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryResponseDto>();
    }
}