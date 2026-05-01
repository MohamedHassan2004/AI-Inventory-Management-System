using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    public class CreateDraftOrderDto
    {
        public OrderType OrderType { get; set; }
    }
}
