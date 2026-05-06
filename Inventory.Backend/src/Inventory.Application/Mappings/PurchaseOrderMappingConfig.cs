using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Domain.Entities;
using Mapster;

namespace Inventory.Application.Mappings
{
    public class PurchaseOrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<PurchaseOrderItem, PurchaseOrderItemResponseDto>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.ProductName, src => src.Product != null ? src.Product.Name : string.Empty);

            config.NewConfig<PurchaseOrder, PurchaseOrderResponseDto>()
                .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : string.Empty);
        }
    }
}
