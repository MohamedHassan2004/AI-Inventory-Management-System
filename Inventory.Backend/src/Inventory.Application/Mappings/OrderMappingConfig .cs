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
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.ProductName,
                    src => src.Product != null ? src.Product.Name : string.Empty); // 🔥 null-safe

            config.NewConfig<Order, OrderResponseDto>();
        }
    }
}