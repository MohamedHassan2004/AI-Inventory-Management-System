using Inventory.Application.DTOs.Order;
using Inventory.Domain.Entities;
using Mapster;

namespace Inventory.Application.Mappings
{
    public class OrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<OrderItem, OrderItemResponseDto>()
                .Map(dest => dest.Id, src => src.Id) // 👈 أهم سطر
                .Map(dest => dest.ProductName, src => src.Product.Name);

            config.NewConfig<Order, OrderResponseDto>();
        }
    }
}
