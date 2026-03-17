using Inventory.Application.DTOs.Category;
using Inventory.Application.DTOs.Product;
using Inventory.Domain.Entities;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Mappings
{
    public class ProductMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Product, ProductResponseDto>()
                .Map(dest => dest.SKU, src => src.SKU);
        }
    }
}
