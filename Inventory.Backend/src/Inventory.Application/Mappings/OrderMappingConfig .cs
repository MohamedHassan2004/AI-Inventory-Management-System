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
                    src => src.Product != null ? src.Product.Name : string.Empty);

            config.NewConfig<OrderItemBatchAllocation, OrderItemBatchAllocationResponseDto>()
                .Map(dest => dest.Quantity, src => src.QuantityTaken);

            config.NewConfig<Order, OrderResponseDto>()
                .Map(dest => dest.CashierName,
                    src => src.Cashier != null? src.Cashier.FullName : string.Empty);

            config.NewConfig<Order, DetailedOrderResponseDto>()
                .Map(dest => dest.CashierName,
                    src => src.Cashier != null ? src.Cashier.FullName : string.Empty);
        }
    }
}