using Mapster;
using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Domain.Entities;

namespace Inventory.Application.Mappings
{
    public class ReturnOrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<ReturnOrder, ReturnOrderResponseDto>()
                .Map(dest => dest.Items, src => src.Items);

            config.NewConfig<ReturnOrderItem, ReturnOrderItemResponseDto>();
        }
    }
}
